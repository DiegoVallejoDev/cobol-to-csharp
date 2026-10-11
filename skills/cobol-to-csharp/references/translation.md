# Phase 2: paragraph-level C# translation

## Shape the target, not a demo

Use the dedicated output root and the user's target conventions; keep C#,
tests, docs, build/oracle copies and runs inside it. Editing an existing target
requires an explicit request. For a new batch target,
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
   Separate data conversion from record reads. Do not repair a mis-declared FD
   or switch file organization to make a case pass. Preserve observed framing,
   WRITE/ADVANCING and CLOSE bytes, not C# WriteLine defaults.
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
7. Build the current increment successfully before any behavioral comparison;
   run its tests, then compare outputs with the original before continuing.
   Update traceability and maps together with implementation changes.

## Generated-code hygiene gate

- [ ] Run `dotnet build` for the current generated project (or the specified
  script host's compilation check). Capture command, diagnostics and exit.
  The compiler catches declaration/type errors cheaply; passing is necessary,
  not evidence of parity. Never compare a stale binary after a failed build.
- [ ] Resolve generated-code build errors before running the comparator. A
  target build error is **failed**; a missing toolchain blocks this gate.
  An unverified draft may be delivered with the gap stated, not fabricated runs.
- [ ] Construct headers, literals and FILLER field-by-field at **declared byte
  offsets and widths** from the map, rather than one hand-counted long string.
  Check encoded byte lengths, not C# character counts. Use only source-backed
  padding/initialization values; preserve retained bytes and intentional overlays.
- [ ] Assert group/record byte totals, field bounds and intended overlaps in
  tests. Check header/filler output as bytes, including trailing spaces.
- [ ] Flag hand-counted space runs as suspect. Replace them with mapped field
  writes or verify their encoded length against the declared layout before use.

Consult [compatibility guidance](compatibility.md) for edge cases and
[GnuCOBOL observations](gnucobol-semantics.md) for probes of edited PIC,
COMP-3 and file IO. Use [verification](verification.md) and
[harness patterns](harness-patterns.md) for evidence rules.

Malformed digits or shifted packed fields may have undefined/runtime-dependent
behavior. Do not invent permissive codecs to manufacture parity. Document the
exact case and limits as **unverified**; unexpected mismatches on defined inputs
remain **failed**. Do not stop translating unrelated, well-defined programs.

## Integration handoff

Report supported and unimplemented paragraphs, external adapters, target
commands, datasets, differences, and verification status. A missing original
runtime permits an explicitly unverified draft, not a verified migration.

In Phase 3, wire programs and job steps, remove truly redundant helpers,
and add regression coverage. Keep known legacy quirks unless separately
authorized to change them. Refactoring must preserve an established baseline;
performance improvements need measurements, not assertions.
