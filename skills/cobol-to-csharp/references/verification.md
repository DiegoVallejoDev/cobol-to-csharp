# Continuous verification and Phase 3 integration

## Establish the oracle

The original COBOL execution is the behavioral reference, not the generated
C#. Use the actual runtime where possible. GnuCOBOL can be a local oracle only
for compatible programs and options; stubs and dialect differences limit any
equivalence claim. Never present local stubs as IBM production certification.

Compile the unmodified source using its established build/dialect first. Keep
compile-defective programs in the requested inventory as **blocked**. Never fix
typos, PICs, business logic or input files merely to make the oracle run.

If the user authorizes a local-compiler adaptation (for example, a missing
PROGRAM-ID terminator), create narrowly patched copies below the output root.
Prefer compatible original options where possible. Record original/patched
hashes, exact diff, diagnostics, compiler options, authorization and why the
adaptation is believed syntax-only. Inspect it; a broad regex can alter source
format, comments, multiline declarations or paragraph boundaries. Results prove
only the **patched source** cases; retain the unmodified compilation blocker.

Record original source/copybook revision or hashes, compiler/runtime versions,
options, platform/encoding, job parameters, dependencies/stubs, sanitized
dataset provenance, and exact expected artifacts. Original and target runs
must use independently staged copies of identical logical inputs and initial
state. A rewriting program must not feed mutated inputs into the other run.
Model intentional multi-step job state consistently on both sides.
Document any FD-aware input conversion and distinguish original physical bytes
from the logical dataset used by a host-encoding oracle. Hash original files
before/after and keep converted copies, builds and captured runs below the output
root, never in the original data/source trees.

If original execution is unavailable, use source-backed contract tests and
review, report **unverified**, and list the precise oracle checks outstanding.
Do not create expected outputs from C# and call them COBOL goldens.

## Build a project-specific comparison harness

Generate it below the dedicated output root using that project's tools, not in
the installed skill directory. Follow [harness patterns](harness-patterns.md)
for stdin bytes, per-case staging, process-group time/output bounds, local .NET
hosts and aggregate verdicts. The harness must:

- Compare every required output, relevant logs, return/abend statuses, and
  observable final state. Separate success cases from intentionally failing
  cases with documented expected outcomes.
- Fail on missing expected or actual artifacts, unexpected failures, malformed
  records, and non-allowed differences. A directory is not an artifact file.
- Use explicit program/case selection. Missing oracle data is not permission
  to skip a required case.
- Preserve process exit codes and pipe failures. Matching files must not hide
  an unexpected nonzero exit; an expected failure still requires matching
  status/partial-output/error contracts.
- Validate record lengths/counts and compare physical bytes when byte contracts
  apply. For databases/services, compare their specified observable semantics.
- Mask nondeterminism only with a reviewed field-level reason from source.
  Validate the masked field's format/domain where specified, and report masks.
  Never trim/pad/normalize output simply to get a pass.

For a predeclared malformed/undefined-data case, record an expected mismatch as
**unverified**, not PASS. Keep it and all compile blockers visible in the full
inventory. Do not downgrade defined-input failures or excuse unrelated differences,
missing artifacts or killed runs. An aggregate with incomplete cases is partial
coverage, not ALL PASS or an ordinary green verification gate.

Retain command logs and differences. Diagnose the first differing byte using
the map: offset/stride, encoding, scale/rounding, signs, initialization, buffer
reuse, key ordering, EOF/group boundaries, or dependency behavior.

## Golden provenance and integration

Never update expected outputs just because the port fails. Intentional
source/input/runtime changes require original-runtime regeneration and a
reviewed explanation. Keep production datasets out of version control.

Wire the requested job/program entry points and dependencies, then run:

1. Target build, configured analyzers/lint/type checks, and unit/contract tests.
2. Original-versus-target comparisons for named datasets and error cases.
3. Integration/regression checks after deduplication and optimization.
4. Actual installation/run commands from the new human guide.

Do not prescribe a solution path, SDK version, or command that was never
created in the target project. When tools are missing, record the commands
not executed rather than reporting them as passed.

## Evidence handoff

Fill [the validation report](../assets/validation-report.md). Include real
commands, exit codes, datasets, artifact outcomes, masks, mismatches and gaps.
Use **verified for named cases**, **unverified**, **blocked**, or **failed**.
No finite dataset establishes equivalence for arbitrary programs or inputs.
