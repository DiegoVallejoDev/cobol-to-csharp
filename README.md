# COBOL → C# modernization pilot

End-to-end proof of the phased migration strategy in
`docs/analysis/`: real IBM-mainframe COBOL (CardDemo `CBACT*` batch
suite, Apache-2.0, vendored under `mainframe/`) ported to C# and
validated **byte-for-byte** against the original running under
GnuCOBOL.

**New here?** [`docs/getting-started.md`](docs/getting-started.md) —
install, one-command run, and what the output looks like (pass *and* fail).

**Using a coding agent?** Start with [`AGENTS.md`](AGENTS.md) and
[`docs/agents.md`](docs/agents.md). Shared instructions and reusable skills
work without Devin; Claude Code has thin project adapters.
See the [improvement audit](docs/improvement-audit.md) for known limitations
and prioritized follow-ups.

## The three phases

| Phase | Where | What |
|-------|-------|------|
| 1. Analyze | `docs/analysis/` | Per-program Markdown: deps, data inputs, error handling, quirks + Mermaid (`dependencies.mmd`, `batch-chain.mmd`) |
| 2. Port | `src/` | `Legacy.Runtime` (owned COBOL primitives: zoned/packed decimal, VSAM-style indexed file, SYSOUT, CEE3ABD) + one faithful file-level port per program in `CardDemo.Batch` |
| 3. Verify | `tests/parity/` | Golden-master harness: run original under GnuCOBOL → capture outputs → run port → diff byte-exact |

`migration-map/` holds the deterministic contract driving the port:
per-record `[offset, length, type]` field maps + file/DD tables +
quirk list, so translation is mechanical and reviewable.

## Ports

| Program | JCL step | Function | Ported semantics worth noting |
|---------|----------|----------|-------------------------------|
| CBACT01C | READACCT | acct → OUTFILE/ARRYFILE/VBRCFILE | COMP-3, OCCURS array, RECORD VARYING RDW, `CALL 'COBDATFT'`, INITIALIZE-vs-MOVE-zero byte difference |
| CBACT02C | READCARD | card file dump | sequential KSDS read |
| CBACT03C | READXREF | xref file dump | DISPLAY inside read paragraph — every record prints **twice** (quirk kept) |
| CBACT04C | INTCALC | interest calc → TRANSACT | composite keys, alt-index read, `/1200` truncation, group-boundary REWRITE, `DEFAULT` fallback, unreachable-ELSE kept bug-for-bug |

## Run it

```bash
# all local checks; does NOT rewrite goldens
bash tools/verify.sh

# run all four C# ports and diff every output byte-for-byte
tests/parity/run_parity.sh  2022071800 mainframe/data              tests/parity/golden
tests/parity/run_parity.sh  2022071800 tests/parity/synthetic/data tests/parity/synthetic/golden CBACT04C

# fresh answer key from original COBOL, outside tracked goldens (needs GnuCOBOL)
scratch="$(mktemp -d)"
tests/parity/run_cobol.sh 2022071800 mainframe/data "$scratch/golden"
tests/parity/run_parity.sh 2022071800 mainframe/data "$scratch/golden"
```

Expected output: every artifact `identical`. TRANSACT records compare
with the two wall-clock timestamp fields masked and format-checked.
Both runners accept optional program names after the first three arguments;
without them all four programs are required. Missing goldens and failed
program exits fail the check. The synthetic goldens cover only CBACT04C.

The synthetic set (`tests/parity/synthetic/gen_data.py`) exercises what
the shipped sample can't: nonzero interest, multi-category accounts,
DEFAULT-group fallback, rate-0 skip, and the dead-ELSE last-account
quirk (last account's interest is computed and written to TRANSACT but
never REWRITEn — in both the original and the port).

Manual invocation mirrors JCL:

```bash
dotnet run --project src/CardDemo.Batch -- CBACT04C <PARM> <data-dir>
```

Use a disposable **staged** data directory; this program rewrites accounts.
See the getting-started guide for staging. The oracle is GnuCOBOL with local
stubs; tested byte parity is not a claim of full IBM runtime/VSAM/EBCDIC
compatibility or equivalence for every possible input.

## Layout

```
mainframe/    vendored CardDemo COBOL/copybooks/JCL/data + license
docs/analysis Phase-1 program documentation + Mermaid
migration-map deterministic field maps for the translator
src/Legacy.Runtime   owned COBOL runtime primitives (not a converter black box)
src/CardDemo.Batch   one class per program, paragraph names preserved
tools/gnucobol       VSAM loaders, JCLDRV PARM emulator, COBDATFT + CEE3ABD stubs
tests/parity         golden-master harness + checked-in goldens + synthetic generator
AGENTS.md / CLAUDE.md shared agent rules + Claude import
.agents/skills       analysis, translation and parity procedures
.claude/skills       thin Claude discovery adapters
tools/verify.sh      provider-independent acceptance gate
```
