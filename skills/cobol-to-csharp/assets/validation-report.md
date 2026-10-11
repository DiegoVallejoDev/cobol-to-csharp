# <SCOPE> — migration validation

Template only. Do not report commands or results that were not executed.

## Verdict

Choose: **verified for named cases**, **unverified**, **blocked**, or **failed**.
State the exact covered source, paragraphs/programs, datasets, and environment.

## Deliverables and traceability

Analysis/map paths, C# files and entry points, adapters, integration structure,
run-guide location, and excluded/unfinished behavior.

## Oracle provenance and input isolation

Original revision/hashes, runtime/compiler/options, encoding, dependencies/stubs,
job parameters, sanitized inputs, initial state, and independent staging.

## Executed checks

| Exact command | Environment / case | Exit code | Actual result |
|---------------|--------------------|-----------|---------------|

List expected failing cases separately, with their required status/error and
partial-output contracts. Distinguish unexecuted commands from failed commands.

## Output comparison

| Artifact / state | Case | Comparison | First difference / evidence |
|------------------|------|------------|-----------------------------|

Include record lengths/counts and masks (field/offset/length, source-backed
reason and validation rule). Missing artifacts are failures, not passes.

## Human run evidence

Actual setup/run commands and captured success/failure output, or an explicit
statement that execution was unavailable. Label illustrative output.

## Gaps and next steps

Unresolved assumptions, unsupported features/services, untested cases/platforms,
blocking tools/access, and any separately authorized behavior changes.
