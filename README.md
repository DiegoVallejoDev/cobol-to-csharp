# COBOL to C# — an AI skill

A portable [Agent Skill](https://agentskills.io/specification) that teaches
your coding agent to **document a COBOL project and translate it into C#
source files/scripts**, with traceable contracts and honest validation.

This repository is the skill itself. It contains no demo application,
vendored mainframe code, generated C# ports, or proprietary agent service.
Bring your own COBOL, copybooks, JCL, and existing coding agent.

## Install

From your **target COBOL project's root**, using the optional
[skills CLI](https://github.com/vercel-labs/skills):

```bash
npx skills add DiegoVallejoDev/cobol-to-csharp --skill cobol-to-csharp
```

Choose your agent when prompted. Node.js is needed only for this installer.
For manual installation, Claude plugin loading, and agent-specific paths,
see [the getting-started guide](docs/getting-started.md).

Or simply tell any file-capable agent to read
[`skills/cobol-to-csharp/SKILL.md`](skills/cobol-to-csharp/SKILL.md) in a
local checkout and apply it to your source project. No installer is required.

## Use

> Use the cobol-to-csharp skill. Document the COBOL under `legacy/`, including
> its copybooks and JCL. Put all generated artifacts under a new
> `migration-output/` folder, leaving original source and data unchanged.
> Then translate the selected programs into C# there, preserving
> behavior. Start with one dependency cluster, validate it against the original,
> and report anything you cannot verify.

You can request **analysis only**, **translation**, **validation**, or the
full three-phase process. The skill first checks which prerequisites exist.

## What it produces in your project

| Phase | Deliverables |
|-------|--------------|
| 1. Document | Per-program Markdown, Mermaid dependency/control-flow diagrams, byte-level JSON maps, error contracts, open questions |
| 2. Translate | C# source files, paragraph-to-method traceability, required project-local adapters, tests and original-output comparisons |
| 3. Integrate | Project structure, deduplication where justified, regression checks, installation/run guide, validation report |

C# normally means `.cs` files in a runnable .NET project. If you want `.csx`,
specify your script host; the skill does not assume one. The skill is a
procedure for an AI agent, **not a deterministic command-line converter**.
Outputs depend on your source, agent, and available evidence.
The output folder contains docs/maps, C#/tests, converted input copies, oracle
builds, isolated runs, harness and run guide. Encoding and physical-versus-declared
record lengths are checked first; blocked/unverified cases stay in the inventory.
Runtime notes include scoped GnuCOBOL probes, not universal COBOL defaults.
See the [one-page output checklist](skills/cobol-to-csharp/checklists/output-artifacts.md)
for the canonical layout and handoff audit.

## Package layout

```text
skills/cobol-to-csharp/
  SKILL.md          # Single entry point
  references/       # Analysis, translation, verification, compatibility
  checklists/       # One-page output-artifact audit
  assets/           # Blank analysis/map/validation templates
.claude-plugin/     # Optional Claude plugin metadata
docs/               # Human installation and usage
tests/              # Package contract tests, not a demo
tools/verify.sh     # Verify this skill package
```

## Check the skill package

```bash
bash tools/verify.sh
```

Requires Python 3 and Bash only. Translating a user's project may additionally
require .NET, its original COBOL runtime, and its external dependencies.
No model calls, cloud credentials, or migrations run during package checks.
