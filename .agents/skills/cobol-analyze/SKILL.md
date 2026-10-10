---
name: cobol-analyze
description: Analyze a COBOL batch program before migration. Use for dependencies, copybook byte layouts, JCL contracts, error paths and migration maps, not for implementing a port.
---

# Analyze before translating

Read repository-root `AGENTS.md` first. Treat paths below as repository-relative.

## Procedure

1. Identify the requested program and scope. Read its source, every included
   copybook, called modules, and JCL steps. Distinguish observed facts from
   assumptions; list unresolved external dependencies.
2. Trace `SELECT`/FD contracts: DD name, organization, access mode, LRECL,
   primary/alternate key offsets and lengths, open mode, output filenames,
   PARM layout, status checks and abend behavior.
3. Calculate zero-based byte offsets from PIC/USAGE definitions. Include
   fillers, implied decimal scale, COMP-3 pad/sign nibbles, OCCURS strides,
   REDEFINES overlays and varying-record bounds. Verify totals against the
   FD/copybook; do not sum overlapping REDEFINES as extra storage.
4. Trace paragraph control flow and mutations, EOF/group boundaries and
   output ordering. Document dead code and quirks instead of correcting them.
   Existing examples are `docs/analysis/CBACT04C.md` and
   `migration-map/CBACT04C.json`; they are not universal templates for
   unsupported COBOL features.
5. Produce/update `docs/analysis/<PROGRAM>.md` (dependencies, input/output,
   flow, errors, quirks, unresolved assumptions) and
   `migration-map/<PROGRAM>.json` (records, offsets/lengths/types, keys,
   files, numeric rules and quirks). Add Mermaid relationships only when
   they add information; validate diagrams with available tooling.
6. Define oracle datasets and expected artifacts before translation:
   happy path, nonzero/negative arithmetic, truncation boundaries,
   DEFAULT fallback, zero rate, EOF/last account, missing files/keys and
   relevant legacy quirks. Existing synthetic data does not cover every
   branch or failure condition.

## Completion

Every output and significant status branch has a source-backed contract.
Field ranges fit the declared record (overlays explicitly identified).
Missing native services/encodings are declared blockers or test limitations,
not guessed away. Hand off the exact source/copybooks, analysis/map paths,
dataset plan, expected masks and unresolved questions to the porting agent.
