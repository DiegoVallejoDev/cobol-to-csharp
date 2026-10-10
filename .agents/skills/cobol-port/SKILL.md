---
name: cobol-port
description: Port an analyzed COBOL batch program to C# paragraph by paragraph while preserving byte-level behavior. Use after a field map and original-COBOL oracle are available, not for business-rule modernization.
---

# Translate against a contract

Read repository-root `AGENTS.md` first. Treat paths below as repository-relative.

## Preconditions

Read the source, copybooks, JCL, analysis and migration map. If a required
map or dependency contract is missing, use `cobol-analyze` first.
Establish original-COBOL output on disposable inputs before changing the port;
use `cobol-parity` for the recipe. Never infer expected bytes from the C# code.

## Procedure

1. Create one program class in `src/CardDemo.Batch/`, dispatched in
   `Program.cs`. Separate orchestration, paragraph methods, file contracts
   and record buffers. Follow existing naming/style without unnecessary
   refactoring of working ports.
2. Translate one paragraph at a time using explicit mapped byte ranges.
   Preserve invocation order, initialization, record reuse, group moves,
   file-status branching and output order. Use existing `Legacy.Runtime`
   codecs/files; financial arithmetic uses `decimal` with source-defined
   truncation and scale.
3. For a missing runtime capability, add a small shared primitive with
   byte-level unit tests and original-COBOL evidence. Do not assume the
   pilot's indexed-file wrapper implements all VSAM semantics, or that
   Latin-1 field helpers constitute full EBCDIC transcoding.
4. Exercise the smallest relevant dataset after each coherent change.
   Compare SYSOUT and every output artifact, not just the final balance.
   Diagnose the first different byte with the field map. Retained bytes,
   initialization and missing ELSE branches can be intentional.
5. Add regression inputs for new behavior and verify them against COBOL.
   Preserve documented bugs. If the user wants a correction, separate the
   compatibility port from the intentional behavior change.
6. Update analysis/map/contracts with the implementation, then run
   `bash tools/verify.sh`. New programs require explicit staging, loader,
   runner and comparator registration; the current harness is a four-program
   pilot, not automatic program discovery.

## Completion

Report migrated paragraphs, shared primitives added, exact checks/exit codes,
datasets and artifact comparisons, and unsupported features. Passing the
sample does not prove parity for all inputs or on IBM mainframe execution.
