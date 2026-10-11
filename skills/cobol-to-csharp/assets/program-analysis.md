# <PROGRAM> — source analysis

Template only. Fill from the user's source; remove inapplicable sections.
Use source paths and paragraph/line references for every significant contract.

## Scope and provenance

- Source, copybooks, JCL, dialect/options, encoding:
- Included/excluded programs:
- Source-backed facts versus assumptions:
- Target format/framework and output paths:
- Dedicated output root and original source/data boundaries:
- Before/after original-file hash manifest:
- Oracle source variant, compiler diagnostics, and any authorized scratch patches:

## Dependencies and diagrams

Static/dynamic calls, external services, DD assignments, PARM/linkage, job order.
Insert validated Mermaid dependency and control-flow diagrams. State validation
method and unresolved dynamic relationships.

## Data layouts

| Record/field | PIC / USAGE | Byte offset | Byte length | Scale / sign / encoding | Source evidence |
|--------------|-------------|-------------|-------------|-------------------------|-----------------|

Account for filler, alignment, OCCURS, and REDEFINES without double counting.
State unknowns instead of guessing values.

## File and parameter contracts

Organization, mode, keys/duplicates, lengths, DD/input/output mapping, parameter
representation, mutations, locks, and record-descriptor conventions.

## Input preflight checklist

- [ ] Check input encoding separately from source encoding, using byte evidence.
- [ ] Classify text/numeric/opaque ranges, including overlays and FILLER.
- [ ] Verify declared record length against actual input size and physical framing.
- [ ] Explain remainders, FD-length drift and partial-record/status behavior.
- [ ] Keep the physical conversion map separate from any inconsistent declared FD.
- [ ] Put converted inputs below the output root, never beside the originals.
- [ ] Verify preserved binary/packed bytes and record input/output hashes.

| File | Size bytes | Physical framing / observed length | Declared FD length | Encoding / conversion evidence | Result / unresolved |
|------|------------|------------------------------------|--------------------|--------------------------------|---------------------|

## Runtime-sensitive probes

Compiler/options/platform, edited-PIC boundary cases, packed sign decoding versus
numeric validation, WRITE/ADVANCING and CLOSE byte behavior. Record the exact
input, command, exit, bytes and source variant; do not adopt another run's defaults.

## Paragraph behavior and failures

Paragraph calls/fall-through, loops, group/EOF boundaries, retained bytes,
statuses, abends, arithmetic size errors, cleanup, and partial output.

## Observable behavior and quirks

Output/state changes, ordering, apparent bugs to preserve, and excluded changes.

## Validation plan and open questions

Named datasets/cases, expected artifacts/statuses, original-runtime availability,
allowed nondeterminism, unresolved dependencies, and blocked paragraphs.
Keep compile-defective and undefined-data cases in scope with explicit verdicts.
Any expected-mismatch marker needs a predeclared case/range, reason and evidence;
it is not a pass.

## Translation traceability

| Source paragraph / evidence | C# file/method | Status | Tests / oracle cases |
|-----------------------------|----------------|--------|----------------------|
