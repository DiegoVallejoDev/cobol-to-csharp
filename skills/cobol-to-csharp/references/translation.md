# Phase 2: paragraph-level C# translation

## Shape the target, not a demo

Use the user's target workspace and conventions. For a new batch target,
prefer a small runnable .NET project, one program-oriented class/file per
COBOL program, explicit input/job contracts, and a test project. Generate
`.cs` files; generate `.csx` only for a specified script host and dependency
model. Do not assume the SDK, solution name, or runtime package is installed.

Create a deterministic skeleton from the analysis: entry point, paragraph
methods, record views, IO interfaces, and dependency adapters. Map each source
paragraph to its method/file and implementation status. Mark unimplemented
behavior explicitly; never use silent success/no-op placeholders.

## Translate in small units

1. Read the source paragraph, applicable copybooks, map, and calling context
   together. Implement a cohesive block, not a guessed restatement of comments.
2. Preserve invocation/fall-through order, loop boundaries, initial values,
   record-buffer reuse, group moves, error handling, and output ordering.
   Do not treat COBOL INITIALIZE as a universal memset or MOVE 0.
3. Use mapped byte ranges for physical records. Keep byte layouts distinct from
   business models so string trimming or serialization cannot change bytes.
4. Implement financial arithmetic using exact representations within the
   required range. Preserve receiving-field scale, truncation, ROUNDED, and
   ON SIZE ERROR. .NET decimal is not automatically sufficient for every PIC;
   use explicit scaled-integer/large-number handling when required.
5. Implement file statuses, keys, duplicate policy, locking/transaction
   requirements, and parameter passing according to the documented contracts.
   Missing dependencies should fail clearly or remain declared blockers.
6. Add only necessary shared primitives in the user's project. Each codec,
   record descriptor, numeric conversion, or IO abstraction needs boundary
   tests and source/runtime evidence. Do not equate flat files with full VSAM.
7. Build and test the increment when the target toolchain is available; compare
   outputs with the original before continuing. Update traceability and maps
   together with implementation changes.

Consult [compatibility guidance](compatibility.md) for edge cases and
[verification](verification.md) for evidence rules.

## Integration handoff

Report supported and unimplemented paragraphs, external adapters, target
commands, datasets, differences, and verification status. A missing original
runtime permits an explicitly unverified draft, not a verified migration.

In Phase 3, wire programs and job steps, remove truly redundant helpers,
and add regression coverage. Keep known legacy quirks unless separately
authorized to change them. Refactoring must preserve an established baseline;
performance improvements need measurements, not assertions.
