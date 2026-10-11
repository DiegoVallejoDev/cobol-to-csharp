# <PROGRAM> — source analysis

Template only. Fill from the user's source; remove inapplicable sections.
Use source paths and paragraph/line references for every significant contract.

## Scope and provenance

- Source, copybooks, JCL, dialect/options, encoding:
- Included/excluded programs:
- Source-backed facts versus assumptions:
- Target format/framework and output paths:

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

## Paragraph behavior and failures

Paragraph calls/fall-through, loops, group/EOF boundaries, retained bytes,
statuses, abends, arithmetic size errors, cleanup, and partial output.

## Observable behavior and quirks

Output/state changes, ordering, apparent bugs to preserve, and excluded changes.

## Validation plan and open questions

Named datasets/cases, expected artifacts/statuses, original-runtime availability,
allowed nondeterminism, unresolved dependencies, and blocked paragraphs.

## Translation traceability

| Source paragraph / evidence | C# file/method | Status | Tests / oracle cases |
|-----------------------------|----------------|--------|----------------------|
