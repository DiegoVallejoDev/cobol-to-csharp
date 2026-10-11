# <SCOPE> — migration validation

Template only. Do not report commands or results that were not executed.

## Verdict

Choose: **verified for named cases**, **unverified**, **blocked**, or **failed**.
State the exact covered source, paragraphs/programs, datasets, and environment.

## Full inventory and verdict counts

| Program / case | Verdict | Compared artifacts / exits | Source variant | Reason / evidence |
|----------------|---------|----------------------------|----------------|-------------------|

Requested total = verified + unverified + blocked + failed. Account for every
requested program/case; do not remove compile defects from the denominator.
Report partial coverage explicitly; do not print ALL PASS with incomplete cases.

## Deliverables and traceability

Analysis/map paths, C# files and entry points, adapters, integration structure,
run-guide location, and excluded/unfinished behavior.

## Oracle provenance and input isolation

Original revision/hashes, runtime/compiler/options, encoding, dependencies/stubs,
job parameters, sanitized inputs, initial state, and independent staging.

Dedicated output root, original source/data boundaries, and before/after hash
manifest. Explain field-aware conversions and verify packed/opaque bytes survived.
Include declared/observed record lengths, input sizes and remainder/status checks.

For a patched oracle: authorization, original and patched hashes, exact diff,
compiler diagnostics and rationale. Label results as patched-source evidence,
not validation of an unmodified program that could not compile.

## Executed checks

| Exact command | Environment / case | Exit code | Actual result |
|---------------|--------------------|-----------|---------------|

List expected failing cases separately, with their required status/error and
partial-output contracts. Distinguish unexecuted commands from failed commands.
Include time/output bounds, timeout/limit flags, stderr, process-group cleanup,
and artifact completeness. Truncated or killed runs are not ordinary passes.

## Undefined-data and expected-mismatch register

| Case | Predeclared reason / byte range | Oracle evidence | Actual differences | Verdict / outstanding checks |
|------|--------------------------------|-----------------|--------------------|------------------------------|

Document malformed/undefined behavior as **unverified**, never PASS. A known
marker does not excuse unexpected mismatches, missing required artifacts or
timeouts elsewhere; those remain failures unless their case contract says otherwise.

## Output comparison

| Artifact / state | Case | Comparison | First difference / evidence |
|------------------|------|------------|-----------------------------|

Include record lengths/counts and masks (zero-based byte offset/length, field, source-backed
reason and validation rule). Missing artifacts are failures, not passes.

## Human run evidence

Actual setup/run commands and captured success/failure output, or an explicit
statement that execution was unavailable. Label illustrative output.

## Gaps and next steps

Unresolved assumptions, unsupported features/services, untested cases/platforms,
blocking tools/access, and any separately authorized behavior changes.
