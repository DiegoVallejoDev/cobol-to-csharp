# Improvement audit

Scope: four CardDemo batch ports, the shared runtime, oracle/parity scripts,
tests, documentation and agent portability. This is a source review plus
local Linux execution, not a production-mainframe certification.

## Verdict

The pilot has a useful byte-level oracle and preserves important legacy
quirks. Its main immediate risk was **false-green validation**, not the
absence of another agent prompt. Cross-agent instructions are valuable
when they lead to a reliable executable gate.

## Fixed in this change

| Finding | Evidence before the fix | Improvement |
|---------|-------------------------|-------------|
| Missing goldens silently passed | Calling `compare.py` with nonexistent run/golden trees returned exit **0** with no output | Default comparison requires all four golden directories and their artifacts; subsets must be explicitly selected |
| Failed programs could still pass | The old synthetic command returned **0** while printing `CBACT02C exit: 16` | Runner retains a failure status even when output bytes match; synthetic verification explicitly runs only CBACT04C |
| Oracle input state differed from C# input state | `run_cobol.sh` ran CBACT04C first and then CBACT01C against the same rewritten account database; C# used independent copies | Fresh indexed inputs per oracle program, optional program selection and unique default scratch directories |
| Guidance depended on a Devin blueprint | No shared agent instruction or named skill files existed | Canonical `AGENTS.md`, importing `CLAUDE.md`, standard skills plus thin Claude adapters and a tool-loading guide |
| Human instructions overstated generality | Getting-started described a program-agnostic harness despite hard-coded staging/program/artifact registration | Explicit pilot scope, registration steps and safe manual staging instructions |

Regression tests cover missing directories/artifacts, explicit program
selection, failed exits despite matching output, byte differences, timestamp
mask boundaries/format, transaction length/count, CRLF staging and overlong
input. A stubbed runner regression checks that a later program does not see
the earlier program's rewritten accounts. Agent-document tests check
imports, skill metadata/adapters and local
links. `bash tools/verify.sh` is the shared, non-regenerating gate.

## Prioritized follow-ups (not implemented here)

These are concrete source-review findings/risks. Their correct behavior
must be established against the relevant COBOL/runtime contract before
changing the compatibility layer.

### 1. Broaden indexed-file contracts before reuse

**High priority for new datasets/programs.**
[`IndexedFile.Open`](../src/Legacy.Runtime/CobolFile.cs) ignores a trailing
partial record and overwrites duplicate primary keys in its sorted map.
Repeated opens do not reset the records, alternate index or sequence cursor.
The alternate-key dictionary retains only one duplicate and `Rewrite`
does not remove the previous alternate-key mapping when that key changes.

Add original-COBOL fixtures and runtime tests for malformed record lengths,
duplicate primary/alternate keys, reopen, read ordering after keyed access,
rewrite key changes and IO errors. Define supported file statuses and
alternate-key behavior explicitly. Do not assume a full VSAM implementation.

### 2. Audit decimal boundaries and invalid input

**High priority for financial generalization.**
[`PackedDecimal`](../src/Legacy.Runtime/PackedDecimal.cs) and
[`ZonedDecimal`](../src/Legacy.Runtime/ZonedDecimal.cs) accumulate scaled
digits in `long`; malformed fields, invalid scales and widths are not
validated comprehensively. Packed writes do not explicitly handle values
wider than the destination digit capacity. Writers add `0.0000001m` before
truncation, which deserves boundary tests rather than an assumption that it
is harmless for every decimal value.

Use a COBOL boundary matrix for sign nibbles, negative zero, invalid digits,
maximum PIC values, overflow/ON SIZE ERROR, rounding/truncation near a cent
and every supported scale. Specify whether invalid input is rejected,
truncated or passed through; do not silently impose new business behavior.

### 3. Make output encoding and host assumptions explicit

**High priority before non-ASCII data or cross-platform claims.**
[`Sysout.Display`](../src/Legacy.Runtime/CobolRuntime.cs) converts record
bytes through Latin-1 to `Console.Out`; it does not directly write Latin-1
bytes. Output encoding/newlines can differ by host. ASCII samples do not
establish fidelity for bytes above `0x7F` or a genuine EBCDIC code page.
The local `COBDATFT`/`CEE3ABD` modules are emulations, not full IBM services.

Add oracle-backed high-byte/text fixtures, explicit SYSOUT encoding/newline
contracts and code-page boundaries. Keep Linux/WSL as the verified path
until native Windows/macOS and IBM-origin datasets have separate evidence.

### 4. Finish deterministic maps and broaden failure coverage

**Medium priority.** Only
[`migration-map/CBACT04C.json`](../migration-map/CBACT04C.json) exists.
Create maps for CBACT01C–03C, validate field ranges/record sizes against
copybooks, and move more magic offsets behind reviewed record contracts.
The nine runtime tests are narrow; parity datasets are not exhaustive.
Add absent files/keys, empty inputs, negative rates/balances, multiple cards
per account and all observable status/abend paths.

### 5. Run the acceptance gate in CI

**Medium priority.** There is no checked-in GitHub Actions workflow.
Add a job for `bash tools/verify.sh` with .NET 8/Python/Bash, followed by
a separate GnuCOBOL job for scratch oracle generation/parity. Do not let
CI regenerate and commit the expected outputs. Upload mismatch artifacts
with explicit retention/access limits if real customer data is later used.

Consider an SDK version policy, analyzers/formatting and shell linting
after the gate is established; avoid broad format churn in compatibility
ports.

## Verification and limits

The acceptance gate verifies runtime unit tests and both checked-in parity
datasets without modifying goldens. Fresh GnuCOBOL goldens were also tested
outside tracked directories, including synthetic CBACT04C followed by
CBACT01C to exercise account-input isolation with nonzero interest.

Agent formats/discovery paths were checked against official documentation
and local structural tests. Devin discovered the repository's canonical
skills and successfully loaded `cobol-parity`. Live loading/invocation in Codex and Claude Code
has not been executed. Skills guide work; they are not proof that an agent
followed the procedure. The executable checks remain the source of truth.
