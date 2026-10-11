# Project-local harness patterns

Generate a harness below the dedicated output root using the user's tools.
This reference is a design/checklist, not an installed runner or runtime.
Read [verification](verification.md) for oracle and golden provenance.

## Inventory first, including programs that cannot run

Create a manifest of every requested program and named case before executing.
Record source variant, exact input/stdin, arguments/environment, independent
staging rules, expected exits and artifacts, byte contracts/masks, time/output
limits and any predeclared undefined-data limitation.

Compile separately from execution. Keep diagnostics for source defects and mark
those programs **blocked**; do not remove them or silently repair a teaching
exercise. A user-authorized scratch syntax adaptation is a separate source
variant, not validation of an unmodified program that cannot compile.

## Independent disposable directories and immutable originals

Use a unique per-case directory under the output root, with separate `oracle/`
and `target/` staging directories. On compatible systems:

```bash
case_root=$(mktemp -d "$output_root/run/case.XXXXXXXX")
```

Ensure the run parent exists first. Preserve failing evidence until reviewed;
do not depend on a global `/tmp` path surviving a restart. Stage identical
initial logical inputs independently; never run against original datasets.
Scope DD assignments, cwd, stdout/stderr, rewritten inputs and all artifacts
to each side. Do not reuse files from a previous case or compare stale outputs.
Hash all originals before/after. Detect path/symlink overlap before writing.

## Stdin means bytes, not escaped text

Prefer a prebuilt byte fixture redirected into each process. For a trusted
literal containing escape sequences:

```bash
printf '%b' '10\n20\n0\n' > "$case_root/stdin"
```

`printf '%s' '10\n20\n0\n'` writes literal backslashes, not three lines.
For already-literal values use `printf '%s\n' "$value"`; do not use `%b` on
untrusted text or assume it handles binary fixtures safely. Capture and hash
the stdin file; pass the same bytes to both processes and close stdin at EOF.
EOF does not necessarily stop ACCEPT loops: some programs continue on spaces.

## Bound the whole run, not just one stdout pipe

Use a process supervisor with:

1. A wall-clock deadline and TERM grace period followed by KILL.
2. A byte budget for **both stdout and stderr**, enforced while draining
   concurrently so neither pipe deadlocks. Bound other artifacts too, using
   a sandbox/quota where needed.
3. A new process group/session for each run. On deadline/output limit, signal
   that group, reap the main process, and ensure no descendants remain.
4. Captured exit/signal, timeout/limit flags, stdout/stderr sizes and exact
   truncation point. A truncated capture is not a complete comparable artifact.

GNU `timeout --signal=TERM --kill-after=2s 10s ...` provides a deadline pattern;
it does not provide an output-byte budget. Confirm available syntax/platform
and group behavior; escaped/detached descendants require stronger containment.
Do not use `--foreground` if relying on group cleanup. Never kill all `cobc`,
`dotnet` or oracle processes by name; only terminate processes owned by this case.

Do **not** use `command | head -c N` as a complete supervisor: it can obscure
exit status, induce SIGPIPE, leave descendants alive and turn capped output into
a misleading comparison. Preserve pipeline statuses if a pipeline is necessary.
Timeouts/limits are failures for a normally terminating case, not proof of parity.
A deliberately nonterminating case needs an explicit bounded contract and must
not be reported as a successful full execution.

## Local .NET installations

For an SDK/runtime deliberately installed under the user's home:

```bash
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --info
```

Use the actual installation path and architecture. Check architecture-specific
DOTNET_ROOT variables when launching apphosts. If a generated apphost cannot find
the runtime, capture stderr and diagnose installation/architecture instead of
treating the exit as a business result. `dotnet path/to/Program.dll` uses the
selected dotnet host; do not assume every exit 131 means a missing DOTNET_ROOT.
Keep target bin/obj and build outputs under the migration output root.

## Compare complete artifacts and narrow masks

Compare expected exit codes **and** stdout, stderr/error contracts and every
declared file/state artifact. Record count, length, first difference and byte
context are useful; a missing file, directory in place of a file, failed build
or unexpected process failure is not an implicit skip.

Define each volatile mask by case/artifact, **zero-based byte offset and length**,
field name, source evidence, reason and a format/domain check on both sides.
For repeated records specify stride/count and validate framing before masking.
Distinguish inclusive ranges from half-open ranges before using reported offsets.
Validate raw artifacts first; compare masked copies, never mutate captured output.
Do not reuse the course's CURRENT-DATE spans without verifying the actual layout.

## Four verdicts, no false-green aggregate

| Verdict | Meaning |
|---------|---------|
| verified for named cases | All required comparisons for these named cases completed and matched the stated oracle variant |
| unverified | Evidence/behavior is limited, including a predeclared undefined-data expected mismatch; not a pass |
| blocked | A source compile defect, unavailable dependency/tool or access prevents the required step |
| failed | An implementation/build/check fails, or a required artifact/status differs unexpectedly |

An **expected-mismatch** register must identify the exact case and affected
contract, source/runtime evidence, reason, owner/authorization and outstanding
work **before comparison**. Capture the mismatch and continue checking unaffected
contracts. An entry neither masks the whole program nor permits missing artifacts,
timeouts or unrelated differences. Unexpected differences still fail; do not
relabel a failing defined-input case just to improve the summary.

Account for the full inventory: requested total equals the sum of the four
verdict counts at that level. Report case counts separately from program counts.
A program with required unverified/blocked cases is not wholly verified just
because another case passed; state partial coverage and gaps.

Example summary format (illustrative, not output of a bundled harness):

```text
Requested programs: 24
Verified for named cases: 18; Unverified: 1; Blocked: 5; Failed: 0
Result: PARTIAL COVERAGE — not ALL PASS
```

For automation, default to success only when **every required case is verified**.
One possible documented exit policy is 0=complete, 1=failed, 2=blocked, 3=unverified
(failure takes precedence, then blocked, then unverified). Always emit all counts.
A separately requested report-only mode may exit successfully after writing an
incomplete report, but must not act as a green parity gate.
