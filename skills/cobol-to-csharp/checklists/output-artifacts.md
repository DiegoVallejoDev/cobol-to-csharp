# Migration-output artifact audit

Canonical **illustrative layout** for a full migration, not captured output.
Keep original source/data outside this root, unchanged. Names may vary; record
actual paths in the validation report. Include only applicable artifacts.

```text
migration-output/
  docs/<PROGRAM>-analysis.md    # Inventory, dependencies, flow, errors, cases
  maps/<PROGRAM>.json           # Byte layouts and paragraph-to-C# traceability
  csharp/                       # Generated projects/adapters, target bin/obj
  tests/                        # Target contract/regression tests
  inputs/                       # Derived fixtures/conversions and provenance
  build/oracle-src/             # Authorized scratch copies and exact patch diffs
  build/oracle-bin/             # Original-runtime binaries
  build/                       # Build commands, diagnostics and exit statuses
  run/<CASE>/{oracle,target}/   # Independent inputs, stdout/stderr, artifacts, exits
  harness/                      # Comparator, case manifest, masks/expected mismatches
  original-hashes.json          # Before/after original-file hashes
  validation-report.md          # Full verdict counts, evidence and gaps
  README.md                     # Install/run commands and captured output
```

## Handoff checks

- [ ] Output root and case paths do not overlap original trees through paths
  or symlinks; originals' before/after hashes match.
- [ ] Analysis/maps cover every requested program and declared field/group,
  including compile defects, encodings, physical/FD lengths and unresolved items.
- [ ] Generated code passed its current build gate before comparison; record
  byte widths/header/filler offsets and tests were checked. No stale-binary pass.
- [ ] Input conversions retain packed/opaque bytes; oracle adaptations have
  authorization, diagnostics, original/patched hashes and exact diffs.
- [ ] Each executed case has independent staging, bounded-run evidence,
  exits/stdout/stderr and every declared artifact. Masks name case/artifact,
  zero-based offset/length, source evidence and raw format/domain validation;
  expected mismatches remain explicit.
- [ ] Undefined-data cases are unverified; source compile defects are blocked;
  defined-input mismatches are failed, never recategorized to pass.
- [ ] Report records commands/environment/hashes, gaps and untested assumptions;
  counts reconcile the full requested inventory. The run guide
  distinguishes captured output from illustrative examples and states limitations.

**Conditional artifacts:** analysis-only stops after analysis/maps and scoped
hash evidence. Do not create oracle patches or converted inputs when unnecessary.
If tools, dependencies or execution are unavailable, document the blocker and
unexecuted artifacts; do not manufacture binaries, runs or successful checks.
For a full migration, any missing required work remains an explicit gap, not a
completed checkbox or a verified result. A complete report may describe partial
coverage; it must not imply ALL PASS.

Use the [translation hygiene gate](../references/translation.md),
[harness patterns](../references/harness-patterns.md) and
[validation-report template](../assets/validation-report.md) for details.
