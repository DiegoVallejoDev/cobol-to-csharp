# Getting started — install, run, and what you'll see

This project ports real IBM-mainframe COBOL to C# and **proves** the port
behaves identically by running the original COBOL and the C# side-by-side
and comparing every output file byte-for-byte. No trust required — the
comparison *is* the demo.

## What you need

| Tool | Why | Install (Debian/Ubuntu) |
|------|-----|--------------------------|
| GnuCOBOL 3 | runs the *original* COBOL to produce the answer key | `sudo apt-get install gnucobol3` |
| .NET 8 SDK | builds and runs the C# ports | `curl -LsSf https://dot.net/v1/dotnet-install.sh \| bash -s -- --channel 8.0 --install-dir ~/.dotnet` then add `~/.dotnet` to `PATH` |
| Python 3 | the staging + comparison scripts | already on most systems |

The supported/verified path is Linux or WSL with Bash. GnuCOBOL is needed
only to produce fresh original outputs, not to compare against checked-in
goldens. Add the installed SDK to your current shell with
`export PATH="$HOME/.dotnet:$PATH"`.

```bash
git clone https://github.com/DiegoVallejoDev/cobol-to-csharp.git
cd cobol-to-csharp
dotnet test          # 9 contract tests on the runtime codecs — should all pass
```

For the complete project acceptance gate, including harness regressions
and both parity datasets, run `bash tools/verify.sh`. It does not regenerate
the checked-in goldens.

## Running the whole pipeline (one command)

```bash
tests/parity/run_parity.sh 2022071800 mainframe/data tests/parity/golden
```

Arguments: `<JCL PARM> <input data dir> <golden dir>`. It stages the flat
sample data into VSAM-style fixed-length files, builds the C# solution,
runs all four ported programs, and diffs every artifact against the
checked-in goldens.

To run the smaller synthetic interest-calculation case, select its one
supported program explicitly:

```bash
tests/parity/run_parity.sh 2022071800 tests/parity/synthetic/data tests/parity/synthetic/golden CBACT04C
```

Without trailing program names the runner requires all four programs.
Missing golden files/directories and nonzero program exits fail validation;
they do not silently reduce test coverage.

What you see when everything is correct:

```
cbact01c:
  sysout.txt: identical (41426 bytes)
  OUTFILE: identical (5350 bytes)
  ARRYFILE: identical (5500 bytes)
  VBRCFILE: identical (2950 bytes)
cbact02c:
  sysout.txt: identical (7626 bytes)
cbact03c:
  sysout.txt: identical (5176 bytes)
cbact04c:
  sysout.txt: identical (5626 bytes)
  TRANSACT: 50 records identical (timestamps masked + format-checked)
  acctdata: identical (15000 bytes)
run dir: /tmp/cbact-cs.5XXyhm
```

Every `identical` line means: the C# program produced *exactly* the bytes
the real COBOL produced — including packed-decimal encoding, EBCDIC
overpunch signs, record-delimiter words, and bug-for-bug quirks.

## What it looks like when something is wrong

If a port diverges — say a field written one byte off — the report names
the artifact, the first differing byte offset, and the raw bytes:

```
cbact01c:
  sysout.txt: identical (41426 bytes)
  OUTFILE: DIFFERS (5350 vs 5350 bytes)
    first differing byte: 100
    line 0: b'00000000001Y00000001940{...' != b'00000000001Y00000001940{...'
```

That is the whole feedback loop: fix the port, re-run, `identical` again.
The temp `run dir` keeps every produced file for manual inspection.

## Running one program by hand

Same interface as the JCL job it replaces (`EXEC PGM=CBACT04C,PARM=...`):

```bash
data_dir="$(mktemp -d)"
python3 tests/parity/stage.py mainframe/data "$data_dir"
dotnet run --project src/CardDemo.Batch -- CBACT04C 2022071800 "$data_dir"
```

The program prints exactly what the COBOL `DISPLAY` statements printed
(sysout) for the tested sample and writes its output files into `$data_dir`.
It also **rewrites `acctdata`**, so use a disposable staged copy as above.
Raw `.txt` inputs and GnuCOBOL indexed database files are not the C# input
format. The runner stages independent copies automatically.

## Pointing it at a different COBOL program

This is a four-program pilot, not an automatic converter. Extending the
same method to a new program requires explicit registration:

1. **Sources** — drop the `.cbl` into `mainframe/cbl/`, copybooks into
   `mainframe/cpy/`, input flat files into `mainframe/data/`.
2. **Compile it for the golden** — register it in the program-selection
   lists and add a run case in `tests/parity/run_cobol.sh`
   (compilation uses `cobc -m -std=ibm -fsign=EBCDIC`).
   Add a JCLDRV/`cobcrun` invocation
   that captures its sysout + output files into `golden/<prog>/`.
   (JCLDRV currently calls CBACT04C; adapt linkage for a new PARM-taking program.
   If it reads new indexed files, write a tiny `LOAD<FILE>` COBOL loader
   like the ones in `tools/gnucobol/` and add the LRECL to
   `tests/parity/stage.py`.)
3. **Port it** — new class in `src/CardDemo.Batch/`, dispatched from
   `Program.cs`. Write the field map first (`migration-map/<PROG>.json`:
   every record's `[offset, length, type]`), then translate
   paragraph-by-paragraph using `Legacy.Runtime` primitives.
4. **Compare it** — register execution in `tests/parity/run_parity.sh`
   and add the program to `tests/parity/compare.py`'s table:
   which artifacts to diff (`sysout.txt` plus each output file) and which
   byte spans to mask (timestamps).

Generate the original's answer key in a scratch directory first, run the
port against it, then review the oracle command/provenance before checking
in new goldens. Never regenerate expected output from the C# port or change
goldens merely to hide a mismatch.

This proves tested behavior against GnuCOBOL plus local runtime stubs,
not production IBM execution or all possible COBOL inputs. See
[the improvement audit](improvement-audit.md) for current limitations.

## Where to look next

- `docs/analysis/` — what Phase 1 documentation looks like per program
- `migration-map/` — the deterministic contract between analysis and code
- `src/Legacy.Runtime/` — the COBOL primitives the ports share (the piece
  you extend for new PIC clauses / file organizations)
- `tests/parity/synthetic/` — how to craft datasets that hit paths the
  shipped sample data can't reach
- [Agent guide](agents.md) — shared instructions, Claude/Codex discovery
  and reusable analysis/translation/parity skills
