# Agent instructions

## Scope and source of truth

This is a verified **CardDemo batch pilot**, not an automatic COBOL converter
or a production mainframe runtime. Four programs (`CBACT01C`–`CBACT04C`)
have C# ports. The oracle is their original source running under GnuCOBOL
with this repository's loaders and stubs, not an IBM production execution.
Do not claim equivalence for untested programs, data, encodings or runtimes.

Read `README.md` and `docs/getting-started.md` for setup. For a program,
read its COBOL, included copybooks, JCL, `docs/analysis/<PROGRAM>.md`,
and migration map before changing its C# port. Only `CBACT04C` currently
has a checked-in map; do not assume missing maps exist.

## Portable setup and acceptance gate

Use Linux or WSL with Bash, Python 3 and .NET 8 SDK on `PATH`. GnuCOBOL 3
with indexed-file support is needed only to run the original COBOL.
No Devin account, proprietary tools, cloud credentials or API calls are
needed for local development. `.devin/blueprint.yaml` is optional setup
convenience, not a prerequisite.

If the SDK was installed in `~/.dotnet`, use
`export PATH="$HOME/.dotnet:$PATH"` in your shell.
Run from the repository root:

```bash
bash tools/verify.sh
```

This checks Bash syntax, agent-doc contracts, Python harness regressions,
a warning-free .NET build, runtime unit tests, full shipped-data parity,
and **CBACT04C-only** synthetic parity. It does not rewrite goldens.
There is no standalone C# formatter/linter configured; compilation is the
current type/analyzer check. Preserve surrounding formatting.

For focused work:

```bash
dotnet test CobolModernization.sln
python3 -m unittest discover -s tests/parity -p 'test_*.py' -v
tests/parity/run_parity.sh 2022071800 mainframe/data tests/parity/golden
tests/parity/run_parity.sh 2022071800 tests/parity/synthetic/data tests/parity/synthetic/golden CBACT04C
```

Both runners accept optional uppercase program names after the first three
arguments. With none supplied, **all four** programs are required. A missing
golden directory/file, failed program exit or byte mismatch is a failure,
not permission to skip a program. The synthetic goldens cover only CBACT04C.

## Compatibility invariants

- Original COBOL/copybooks/JCL define semantics. Do not edit `mainframe/`
  to make a port pass; preserve the vendored license and provenance.
- Use `decimal` for financial calculations, not `double`/`float`.
  Preserve implied scale, truncation, overpunch and packed-decimal signs.
  Check `ROUNDED`/`ON SIZE ERROR` in the original before changing arithmetic.
- Records are bytes with fixed offsets, not trimmed Unicode strings.
  The pilot uses Latin-1 field mapping and ASCII-hosted EBCDIC overpunch
  (`-fsign=EBCDIC`), **not a full EBCDIC code-page converter**.
- Account records are 300 bytes; account IDs/keys are **11 bytes**.
  Card records are 150; xref, discount and category-balance records are 50;
  transactions are 350. Verify each field against the copybook.
- COMP-3 uses packed digits and a trailing sign nibble (`C`/`D`);
  even digit counts have a leading pad nibble.
- Variable-record output uses the oracle's four-byte descriptor:
  two-byte big-endian payload length plus two zero bytes.
- Preserve file-status branches and paragraph order. Keep `Legacy.Runtime`
  shared and small; do not copy runtime helpers into individual ports.
- Keep known quirks: CBACT01C's missing debit-field `ELSE` and
  INITIALIZE-versus-MOVE-zero bytes; CBACT03C's duplicate DISPLAY;
  CBACT04C's unflushed last account and no-op fee paragraph.
  Business corrections require a separate, explicitly approved change.
- Only TRANSACT's two 26-byte timestamp spans (offsets 278 and 304) are
  masked; both sides must have the expected format. Never widen masks,
  trim outputs or relax checks to hide a mismatch.

## Safe oracle and manual execution

Run ports only on **disposable staged copies**. CBACT04C rewrites
`acctdata`; programs create/overwrite outputs in the supplied directory.
`python3 tests/parity/stage.py <txt-input-dir> <disposable-dir>` creates
the fixed-length input files; raw `mainframe/data/*.txt` and GnuCOBOL's
indexed database files are not interchangeable with these files.

For a fresh answer key, write outside checked-in goldens:

```bash
scratch="$(mktemp -d)"
tests/parity/run_cobol.sh 2022071800 mainframe/data "$scratch/golden"
tests/parity/run_parity.sh 2022071800 mainframe/data "$scratch/golden"
```

Use `COB_LIBRARY_PATH` with bare module names in `cobcrun`; the runner
handles compilation flags, DD variables and JCL PARM linkage.
Default scratch directories are unique and printed for diagnosis.
Parallel oracle runs must not share an explicitly overridden `COB_DIR`.

Never generate an answer key from C#. Change checked-in goldens only for
an intentional source/input/stub change, with original-COBOL provenance,
the exact command and a reviewed explanation of the artifact differences.
Do not regenerate them just because a comparison fails.

## Reusable procedures and handoff

Read the matching canonical Agent Skill when needed:

- `.agents/skills/cobol-analyze/SKILL.md`: dependency analysis and field maps.
- `.agents/skills/cobol-port/SKILL.md`: paragraph-level C# migration.
- `.agents/skills/cobol-parity/SKILL.md`: oracle execution and mismatch diagnosis.

`CLAUDE.md` imports this file; `.claude/skills/` contains thin adapters.
Do not duplicate project rules there. For other agents, loading instructions
is tool-specific; use `docs/agents.md` rather than assuming auto-discovery.

Use a focused branch/PR, not direct pushes to `main`; do not merge without
permission. Do not commit scratch data, build outputs or credentials.
Handoff: changed programs/paragraphs, map and analysis updates, exact
validation commands and exit codes, tested datasets, unresolved gaps.
If a tool is unavailable, report what could not be verified rather than
claiming success. Known follow-ups are in `docs/improvement-audit.md`.
