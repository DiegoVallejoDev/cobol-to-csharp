---
name: cobol-parity
description: Verify COBOL-to-C# parity and diagnose output differences using the original GnuCOBOL oracle. Use for test execution, new datasets, missing artifacts, failed exits and golden provenance, not for changing business semantics.
compatibility: Requires Bash, Python 3 and .NET 8; fresh COBOL execution additionally requires GnuCOBOL 3 with indexed-file support.
---

# Prove behavior, do not approve a false green

Read repository-root `AGENTS.md` first. Treat paths below as repository-relative.

## Checked-in baseline

```bash
bash tools/verify.sh
```

This never regenerates goldens. If .NET is in `~/.dotnet`, add it to `PATH`.
Synthetic baseline coverage is **only CBACT04C**, selected explicitly by
the verification script. Both runners default to requiring all four programs.

## Fresh original-COBOL evidence

To verify the shipped data without modifying tracked answer keys:

```bash
scratch="$(mktemp -d)"
tests/parity/run_cobol.sh 2022071800 mainframe/data "$scratch/golden"
tests/parity/run_parity.sh 2022071800 mainframe/data "$scratch/golden"
```

For the synthetic interest-calculation case:

```bash
scratch="$(mktemp -d)"
tests/parity/run_cobol.sh 2022071800 tests/parity/synthetic/data "$scratch/golden" CBACT04C
tests/parity/run_parity.sh 2022071800 tests/parity/synthetic/data "$scratch/golden" CBACT04C
```

Always run both sides on the same source inputs and PARM, with independently
staged per-program copies. Do not feed one side's rewritten accounts to the
other. The runners print retained scratch directories. Golden account output
is named `acctdata.bin`; the port output is `acctdata`.

## Diagnose a failure

1. Preserve the real exit status (use `set -o pipefail` if piping logs).
   A nonzero program exit fails even if emitted artifacts happen to match.
   Missing directories/files are failures, not an implicit program selection.
2. Confirm dataset, PARM, LRECL, staging, DD assignments, keys, oracle/stub
   version and input isolation. Never point a rewriting port at original data.
3. Locate the reported first byte or differing TRANSACT record. Map it to
   a field; check alignment, padding, overpunch, COMP-3, RDW byte order,
   implied scale/truncation, retained buffers and source control flow.
4. Only TRANSACT's 26-byte timestamps at 278 and 304 are masked. Their
   format is checked on both sides; this is not calendar-validity checking.
   Do not mask additional fields or normalize SYSOUT to force a pass.
5. Fix the C# port/harness, not the original source or oracle output.
   If a source/input/stub change intentionally invalidates tracked goldens,
   obtain agreement on that change and regenerate them from COBOL with
   a reviewed provenance/diff explanation.
6. Add a regression for the discovered gap and rerun relevant datasets
   plus the full acceptance gate.

## Handoff

Give commands and exit codes, input/PARM and program selection, artifact
results (including timestamp masks), original-COBOL provenance and untested
gaps. The oracle uses GnuCOBOL and local stubs; do not describe this as a
production IBM-mainframe certification.
