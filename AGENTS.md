# Contributing to the skill

This repository is an **AI skill**, not a COBOL demo, converter executable,
or C# application. Its product is `skills/cobol-to-csharp/`: instructions,
references, and templates that a coding agent uses in a separate user project.
Do not add sample applications, vendored COBOL estates, generated ports,
runtime libraries, datasets, or golden outputs here.

## Source of truth

- `skills/cobol-to-csharp/SKILL.md` is the single skill entry point.
- `references/` contains on-demand procedures and compatibility guidance.
- `assets/` contains blank deliverable templates, not completed migrations.
- `README.md` and `docs/getting-started.md` explain installation and use.
- `CLAUDE.md` imports these contributor rules; it is not the consumer skill.
- `.claude-plugin/plugin.json` exposes the same skill as a Claude plugin.

Keep the complete skill directory independently installable. Its instructions
must not depend on this repository's root files, test tools, absolute paths,
Devin APIs, or one agent's private tool names. Resources are relative to the
skill directory; generated output belongs in the user's target workspace.

## Scope

Preserve the three phases: document COBOL, translate to C# source/scripts,
then integrate and validate. Start parity work during translation, not only
after integration. Derive record sizes, offsets, encodings, numeric semantics,
and file behavior from the user's source and dialect, never from a demo.

The skill must distinguish source-backed facts, assumptions, and blockers.
Never claim equivalence without original-COBOL evidence. When execution is
unavailable, allow explicitly unverified translation instead of inventing
results. Business corrections require separate authorization.

## Validation

Only Python 3 and Bash are needed to check this package:

```bash
bash tools/verify.sh
```

This checks Python syntax, standard-library tests, metadata, local links,
template contracts, isolated copy installation, and absence of demo assets.
It does **not** run a migration or prove a model can translate arbitrary COBOL.
No .NET, GnuCOBOL, formatter, or external packages are required for this repo.
Use consistent Markdown/Python formatting and run `git diff --check`.

Work on a focused branch/PR; do not push to main or merge without permission.
Report exact checks and limitations, especially untested agent clients.
