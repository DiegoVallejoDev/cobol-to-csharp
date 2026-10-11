# CBL0001: documentation-to-validation walkthrough

This is a **documentation example**, not a bundled application or converter.
It uses [CBL0001 at course revision 1f037d2](https://github.com/openmainframeproject/cobol-programming-course/blob/1f037d24326124f4f2c5cd2d31e5220ceb549580/COBOL%20Programming%20Course%20%232%20-%20Learning%20COBOL/Labs/cbl/CBL0001.cobol).
Credit: Contributors to the COBOL Programming Course, source marked
[CC-BY-4.0](https://creativecommons.org/licenses/by/4.0/).

One synthetic case was independently executed with GnuCOBOL **3.1.2.0** on
Linux and .NET SDK **8.0.425**. No course dataset or full-corpus run was reproduced.
The source, generated target and fixtures lived in an isolated probe workspace;
none are included in this skill package.

## 1. Analyze source and data before translating

CBL0001 opens ACCTREC for input and PRTLINE for output. It reads once before
the loop, writes each account's selected fields, reads again, then closes at
EOF. PRINT-LINE uses fixed sequential records and a plain WRITE.

The input FD declares:

| Field | Offset | Bytes | Representation |
|-------|--------|-------|----------------|
| ACCT-NO | 0 | 8 | X(8) |
| ACCT-LIMIT | 8 | 5 | S9(7)V99 COMP-3 |
| ACCT-BALANCE | 13 | 5 | S9(7)V99 COMP-3 |
| LAST-NAME | 18 | 20 | X(20) |
| FIRST-NAME | 38 | 15 | X(15) |
| STREET-ADDR | 53 | 25 | X(25) |
| CITY-COUNTY | 78 | 20 | X(20) |
| USA-STATE | 98 | 15 | X(15) |
| RESERVED | 113 | 7 | X(7) |
| COMMENTS | 120 | 50 | X(50) |

Total **170 bytes**. The output is 8 + 13 + 13 + 20 + 15 + 50 =
**119 bytes** per record. Both numeric outputs use `$$,$$$,$$9.99`.
Record these in the analysis/map templates, with source paragraph/line references.
Do not assume every CBL-family FD has this layout.

The named fixture `synthetic-account` contained one 170-byte record:
account `00000001`, limit 100000.00, balance 123.45, last/first name DOE/JANE,
address `1 MAIN ST`, city `NEW YORK`, state `NY`, blank RESERVED and comment
`TEST CASE`. Text was padded to the declared widths and encoded cp037.
The packed fields were `01 00 00 00 0C` and `00 00 12 34 5C`.

For this known text fixture, convert byte ranges [0, 8) and [18, 170) from
cp037 to ASCII; preserve [8, 18) exactly. The derived input remains 170 bytes.
Keep raw and converted hashes, verify preservation and stage converted ACCTREC
independently on each side. Do not apply this conversion blindly to other data.

## 2. Identify the original compilation blocker

The pinned source lacks a terminating period after PROGRAM-ID. The unmodified
local compile exited **1** with a diagnostic including:

```text
error: syntax error, unexpected ENVIRONMENT, expecting COMMON or EXTERNAL or RECURSIVE or INITIAL
```

For this isolated experiment, a copy was adapted from
`PROGRAM-ID.    CBL0001` to `PROGRAM-ID.    CBL0001.`; everything else was
unchanged. Original-source bytes were checked unchanged afterward.

Captured SHA-256 provenance:

| Artifact | SHA-256 |
|----------|---------|
| Original source | `99bb990cd6d5a6b210d466ae302cf0fbc40a8a3b8e7f532bcc28ba94ff6161a5` |
| Syntax-adapted source | `afe3b66fb4145dab1fdbaf64c7800094c2e5e464e610ac030fc96372e183ffe1` |
| Raw synthetic input | `6109728fc7460f82e20b7d9a79f1cbdce206472ac634fae5ff0374020325a05a` |
| Field-converted input | `a6a97639df186cc91abed61b98583d81561e6debef702edee55d760eb00e03d8` |

In a user's project, obtain authorization before this adaptation, store the
copy/diff below the output root and record both hashes. Preserve the original
compilation result as **blocked**. Do not use this pattern to repair course
typos or change business logic. Parity below is against this **syntax-adapted
copy**, not evidence that the unmodified source runs.

## 3. Translate the source contracts into C#

Generate a project below the output root, map source paragraphs to target
methods/blocks, and preserve the initial read and EOF sequence:

| Source behavior | Target responsibility |
|-----------------|-----------------------|
| OPEN-FILES | Open staged ACCTREC, create case-local PRTLINE |
| READ-NEXT-RECORD / READ-RECORD | Read fixed 170-byte chunks; process each record once |
| WRITE-RECORD | Copy mapped text slices and edit both packed fields into 13-byte outputs |
| CLOSE-STOP | Close handles; return the documented exit status |

The experimental target used explicit byte IO, a nine-digit packed magnitude helper
restricted to C/D signs, and invariant edited formatting; it did not insert
line feeds. Its partial-record and invalid-data policy is **not** validated by
this one well-formed case. Real migrations need those oracle cases too.
Add primitive tests for encoding, packed signs, numeric boundaries and edited
currency before generalizing. See [runtime notes](gnucobol-semantics.md).

This excerpt from the generated C# writes one complete input record; `record`
is a 170-byte buffer and `output` is the case-local stream. `WriteEdited` is
the generated helper for this source's specific packed/edited contract, not a
library provided by the skill:

```csharp
output.Write(record.AsSpan(0, 8));
WriteEdited(record.AsSpan(8, 5));
WriteEdited(record.AsSpan(13, 5));
output.Write(record.AsSpan(18, 20));
output.Write(record.AsSpan(38, 15));
output.Write(record.AsSpan(120, 50));
```

The output fields omit address/state/RESERVED exactly as the COBOL does.

## 4. Run independent staged copies and compare

The following are the executed invocations with the isolated path replaced by
`$probe_root` and the installed host by `$DOTNET_ROOT/dotnet`. Project/fixture
names belong to this generated experiment; these commands are **not** a
ready-made runner supplied by the installed skill.

```bash
cobc -x "$probe_root/CBL0001-patched.cobol" -o "$probe_root/cbl0001"
"$DOTNET_ROOT/dotnet" build "$probe_root/Walkthrough.csproj"

# cwd: $probe_root/worked/oracle; the inputs are independent copies
DD_ACCTREC="$probe_root/worked/oracle/ACCTREC" \
DD_PRTLINE="$probe_root/worked/oracle/PRTLINE" \
  "$probe_root/cbl0001"

# cwd: $probe_root/worked/target
"$DOTNET_ROOT/dotnet" "$probe_root/bin/Debug/net8.0/Walkthrough.dll" \
  "$probe_root/worked/target/ACCTREC" "$probe_root/worked/target/PRTLINE"
```

Both executions were captured with a five-second deadline. Build exited 0
with **0 warnings and 0 errors**. Captured run evidence:

```text
oracle exit 0 stdout b'' stderr b''
target exit 0 stdout b'' stderr b''
PRTLINE bytes 119 sha256 736a7ed82616841f4003aadd1f02c95276d7c50414ccf872d1faaafd41cba330
Original input unchanged; no masks
```

The oracle's PRTLINE and the target file were byte-identical. Their first
34 bytes were `b'00000001  $100,000.00      $123.45'`; fixed-width names and
comments followed, with no final newline. The expected artifact came from
GnuCOBOL, not from the C# implementation.

## 5. State the result honestly

**Verified for the named synthetic-account case against the syntax-adapted
GnuCOBOL source.** Unmodified-source execution remains **blocked** in this local
setup. Empty/multiple records, partial records, malformed numbers, alternate
signs, overflow, other encodings and platforms remain unverified.

For a real project, put the analysis/map, generated C#/tests, converted inputs,
authorized oracle copies, runs, harness and human guide under one new output
root. Keep source/data unchanged with before/after hashes. Fill the
[validation report](../assets/validation-report.md) and include every requested
program/case; this worked case does not stand in for corpus-wide validation.
