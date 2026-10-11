# Phase 1: source-backed documentation

## Inventory and dependency graph

Read each selected program and all recursively included copybooks. Resolve
COPY search paths and REPLACING clauses using the actual dialect/build setup.
Record missing includes instead of silently substituting a similarly named file.

Identify static and dynamic CALLs, ENTRY points, LINKAGE parameters, return
codes, shared records, and transaction boundaries. Read JCL for step order,
DD assignments, DISP, PARM, sorting/utilities, and conditional execution.
Include database, queue, CICS, filesystem, and assembler/service contracts.
Dynamic targets and unavailable services remain explicit open dependencies.
Keep every requested program in the inventory, including programs that fail
compilation. Course fixtures may deliberately contain typos or invalid PICs;
record diagnostics as **blocked**, not a reason to silently omit or repair them.

Create Mermaid dependency and paragraph/control-flow diagrams. Use neutral
node IDs and quoted labels. Diagram only relationships supported by source;
distinguish dynamic/unresolved calls. Validate Mermaid with available tooling;
if only inspected manually, state that rather than claiming parser validation.

## Data and storage

For each FD, SD, working-storage group, and linkage contract:

- Calculate **zero-based byte** offsets and storage lengths, including filler,
  sign representation, implied decimal scale, synchronization/alignment, and
  compiler-specific binary widths.
- Capture PIC, USAGE, sign, encoding, initialization and padding. Preserve
  the distinction between elementary numeric values and group byte moves.
- Record OCCURS bounds/stride and OCCURS DEPENDING ON. Model REDEFINES as
  overlays, not additional sequential bytes. Level-88 conditions are predicates,
  not stored fields.
- Record fixed/varying lengths, keys (offset, width, collating sequence,
  duplicates), organization, access/open modes, and file statuses.

Check storage totals against source and compiler evidence. See
[compatibility guidance](compatibility.md). Unknown lengths/offsets remain
unknown and block implementations that depend on them.

## Input preflight gates

Before conversion or translation:

- Establish **data encoding**, separately from source-file encoding. Inspect
  hex samples, file provenance, and field categories; ASCII-looking views are
  not evidence that the binary dataset is ASCII.
- Derive any conversion from verified FD/copybook ranges. Convert only ranges
  proven to be text; preserve COMP-3, binary and opaque bytes verbatim unless
  an explicit representation conversion is required. PIC X can contain opaque
  bytes and REDEFINES can overlay text and numbers. DISPLAY numerics/signs need
  their own proven encoding/sign mapping. Never blanket-iconv a mixed record.
- Record file size, physical organization/framing and observed record length.
  Compare with **declared FD length**, including descriptors where applicable.
  A remainder on a fixed headerless file is a warning, not permission to pad,
  truncate, repair the FD, or reinterpret it as line sequential.
- Explain inconsistent layouts explicitly: the conversion map for the actual
  physical dataset and the program's declared read map may differ. Record both.
  Variable, indexed, blocked and header-bearing files need their own framing
  analysis; divisibility alone cannot establish record length.
- Record conversion rules, input/output hashes and field preservation checks.
  Write converted copies only below the dedicated output root; stage the same
  documented logical input independently for oracle and C#.

See [GnuCOBOL examples and limitations](gnucobol-semantics.md) for packed/text
records and FD-length drift. Unknown encoding or framing blocks **verified**
translation of affected reads; it does not block unrelated analysis.

## Behavior and failures

Trace paragraph entry/exit, PERFORM/THRU ranges, fall-through, GO TO targets,
EOF and group boundaries, buffer reuse, rewrites, output ordering, and cleanup.
Map each READ/WRITE/REWRITE/START and arithmetic status branch, including
ON SIZE ERROR, INVALID KEY, AT END, abends, and partial output on failures.
Document unreachable or suspicious behavior without correcting it.

Every significant finding should reference a source path and paragraph/line
range. Separate observed behavior, inferred intent, and unresolved assumptions.
Do not take comments as proof when executable statements contradict them.

## Deliverables and test design

Fill [the analysis template](../assets/program-analysis.md) and
[the field-map template](../assets/migration-map.json) below the output root.
Arrays in the map describe actual records/files/paragraphs, not requirements
to include nonexistent features. Replace nulls when established, or accompany
remaining nulls with a precise unresolved item.

Plan inputs and expected artifacts for happy paths, empty/missing inputs,
missing/duplicate keys, invalid records, numeric boundaries, negative/zero
values, rounding/truncation, last-group/EOF behavior, and external failures.
Identify which cases require the original runtime. Use sanitized fixtures and
disposable copies; do not commit production records or secrets.

Hand off analysis, map, dependency contracts, proposed datasets, unknowns,
and the smallest source-verified translation unit.
