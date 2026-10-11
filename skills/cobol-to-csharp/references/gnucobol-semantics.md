# GnuCOBOL: byte-level observations, not universal rules

Use the [GnuCOBOL Programmer's Guide](https://gnucobol.sourceforge.io/HTML/gnucobpg.html)
sections on file organization, WRITE, PICTURE, USAGE and compiler options.
Record the installed version/options and reproduce relevant cases locally.
The guide spans versions; neither it nor a course run certifies an IBM runtime.

The course field report motivated these notes. The small probes below were
independently run on Linux with **GnuCOBOL 3.1.2.0**, `cobc -x -free`, default
options, ordinary disk files, no LINAGE or printer assignment. They are evidence
for those probes only, not a reproduction of the user's full 24-program run.
`b'...'` notation denotes actual bytes; `\n` is one byte `0A`.

## Mixed EBCDIC text and packed data

A record is not necessarily one encoded string. A course account layout has
170 bytes, including two five-byte `S9(7)V99 COMP-3` fields. A blanket cp037
decode/re-encode or `iconv` corrupts those packed bytes.

Use verified physical field ranges to convert text to the oracle's required
host encoding, leaving packed/binary/opaque ranges unchanged. For this layout,
the packed spans are **[8, 13)** and **[13, 18)** (zero-based, end exclusive).
Do not copy these offsets into another program: establish its own map first.
Confirm the actual EBCDIC code page; cp037 is not a default for every dataset.
PIC X is not proof of text, and DISPLAY numerics/overpunch may need separate
conversion. Preserve byte widths and verify untouched ranges byte-for-byte.

Keep the raw dataset unchanged. Store the converter, derived file, map, hashes
and provenance below the output root. Feed equivalent independently staged
inputs to oracle and target. If physical and declared FD maps disagree, do not
use the wrong FD to "correct" the physical data.

## WRITE, ADVANCING and CLOSE

For two three-byte records `ABC`, `DEF`, the local probes produced:

| Organization | WRITE form used for both records | Bytes after CLOSE |
|--------------|----------------------------------|-------------------|
| SEQUENTIAL, RECORDING MODE F | no ADVANCING | `b'ABCDEF'` |
| SEQUENTIAL, RECORDING MODE F | AFTER ADVANCING 1 LINE | `b'\nABC\nDEF\n'` |
| SEQUENTIAL, RECORDING MODE F | AFTER ADVANCING 2 LINES | `b'\n\nABC\n\nDEF\n'` |
| SEQUENTIAL, RECORDING MODE F | BEFORE ADVANCING 1 LINE | `b'ABC\nDEF\n'` |
| LINE SEQUENTIAL | no ADVANCING | `b'ABC\nDEF\n'` |

For these sequential AFTER cases, advancing writes introduce the requested
number of line feeds before a record; CLOSE contributes a final line feed.
This is **not** "`n LINES` always writes one newline", nor does a plain WRITE
omit the newline on every organization.

Probe empty files, 0/1/multiple lines, BEFORE versus AFTER, mixed plain/advancing
writes, final CLOSE, trailing spaces and PAGE/LINAGE when used. Record assignment
(printer versus ordinary file), runtime configuration and platform too. Line
sequential trimming, escaping, record splitting and Windows line endings need
their own contracts. Do not implement all COBOL files using `StreamWriter.WriteLine`.

## Edited PIC and floating currency

Local MOVE/display probes for `$$,$$$,$$9.99` and `ZZ,ZZZ,ZZ9.99`:

| Numeric value | Currency picture (13 bytes) | Z picture (13 bytes) |
|---------------|-----------------------------|----------------------|
| 100000 | `b'  $100,000.00'` | `b'   100,000.00'` |
| 0 | `b'        $0.00'` | `b'         0.00'` |
| -123.45 | `b'      $123.45'` | `b'       123.45'` |

For these pictures, digits are placed right-to-left, `9` positions print even
for zero, leading suppressible positions become spaces, and floating `$`
occupies the position immediately left of the first printed digit. That
position can be a suppressed comma: in `  $100,000.00`, byte 2 is `$`.
It is not a formatter that can place `$` only in literal `$` picture slots.

These unsigned edited pictures did **not** print the input minus sign. Do not
generalize that to signed edited pictures. Probe zero, cents, negative values,
comma boundaries, maximum width, overflow/truncation and rounding. Currency
configuration, decimal-point conventions, CR/DB and BLANK WHEN ZERO matter.
Do not rely on locale-dependent C# currency formatting or Trim/PadLeft alone.

## COMP-3: layout versus decoder/validation policy

`S9(7)V99` has nine digit nibbles plus one sign nibble: **five bytes**.
For example, `01 23 45 67 8C` encodes a magnitude of 123456.78 with a positive
sign. Even digit counts require a leading pad nibble; scale does not consume
a stored decimal-point byte.

Do not equate conventional sign meanings with every runtime operation.
Traditional tables may name C/F positive and B/D negative, but the local
GnuCOBOL signed-field probes produced:

| Final nibble in `01 23 45 67 8?` | MOVE to `+9(7).99` | `IS NUMERIC` |
|--------------------------------|--------------------|--------------|
| C | `+0123456.78` | true |
| F | `+0123456.78` | false |
| B | `+0123456.78` | false |
| D | `-0123456.78` | true |

These are observed results, not recommended permissive decoding rules. The
field report's B-as-negative rule is not reproduced by this default probe.
Options such as `-fhostsign` can affect numeric validation; probe actual MOVE,
DISPLAY, arithmetic and `IS NUMERIC` paths separately. State accepted digits,
pad/sign rules and invalid-data policy. Never silently map invalid digits to zero.

## FD length and physical record drift

For a fixed, headerless dataset of **45 x 170 = 7650 bytes**, an FD of 174 bytes
gives **43 complete chunks and a 168-byte remainder**. Each full read starts
four bytes further into the next physical record. It does not read "45 logical
records and ignore four filler bytes".

The field report observed 43 records in CBL0010. A separate local sequential
READ probe with FILE STATUS and AT END/NOT AT END counted **44 reads**, including
a final partial read with status **04**, then EOF status **10**. Thus file-size
arithmetic diagnoses drift; it does not by itself predict the program's count.
Check exact READ status handling, buffer contents and short-record behavior.

Never silently change the FD, pad the input or drop the remainder. Document
the physical layout, declared layout and observed statuses separately. A shifted
packed field can contain non-decimal nibbles; successful execution does not
establish a meaningful business contract for those bytes.

## Undefined data and compile defects

Malformed numeric bytes can expose runtime/version-dependent operations, such
as DISPLAY passing non-digits or invalid nibbles becoming punctuation. Do not
implement a general nibble-to-character rule from an accidental result. Use
**unverified** for documented cases whose behavior cannot be defensibly
specified, rather than engineering a fake pass. Defined-input mismatches remain
**failed**. See [harness verdicts](harness-patterns.md).

Intentional course defects stay **blocked** with compiler diagnostics and source
references. A typo, wrong receiving PIC, case-sensitive comparison quirk and
missing PROGRAM-ID terminator are not all the same problem. Do not auto-fix them.
For an authorized local-compiler adaptation, use narrowly patched copies below
the output root, hashes and an exact diff. Label the oracle **patched source**;
it cannot prove unmodified-source parity. See [oracle provenance](verification.md).
