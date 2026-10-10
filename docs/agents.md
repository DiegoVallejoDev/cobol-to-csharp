# Working with any coding agent

You do not need Devin to run or extend this project. It is a local
.NET/Bash/Python repository. GnuCOBOL is needed for fresh legacy executions;
the checked-in-baseline verification needs only .NET 8, Python 3 and Bash.
Use Linux or WSL; native Windows/macOS execution is not verified.

## Three different jobs, three different files

| File | Purpose | When loaded |
|------|---------|-------------|
| [`AGENTS.md`](../AGENTS.md) | Shared project rules, setup, validation, byte invariants and safety | At the start of work, by supporting agents or explicit instruction |
| [`CLAUDE.md`](../CLAUDE.md) | Claude Code adapter importing `AGENTS.md` with `@AGENTS.md` | Claude Code's project instructions |
| [`.agents/skills/`](../.agents/skills/) | Named, reusable procedures with standard `SKILL.md` frontmatter | On demand, not all at startup |

A lone root `SKILL.md` is not a substitute for repository instructions.
Agent Skills use a **named directory with a `SKILL.md`**, including `name`
and `description` metadata. Keeping always-on rules short and procedures
on demand avoids loading every workflow into every conversation.

## Discovery by tool

| Tool | Project instructions | Skills |
|------|----------------------|--------|
| Codex | Root `AGENTS.md` | Canonical `.agents/skills/<name>/SKILL.md`; select/mention `$cobol-analyze`, `$cobol-port` or `$cobol-parity` |
| Claude Code | `CLAUDE.md` imports `AGENTS.md` | `.claude/skills/<name>/SKILL.md` adapters; `/cobol-analyze`, `/cobol-port`, `/cobol-parity` |
| Devin | Ask it to read `AGENTS.md`; optional `.devin/blueprint.yaml` provisions the toolchain | Canonical skills were discovered locally and `cobol-parity` was invoked in this session; explicit file reading remains a fallback |
| Any other agent | Ask it to read `AGENTS.md` first unless its loader explicitly supports it | Ask it to read the relevant canonical skill file |

The Claude adapters are short instructions to read the canonical procedure,
not duplicated workflows or symlinks. They work in ordinary Git checkouts
without symlink support. Tool loading can vary by version/settings; these
paths/formats follow the linked specifications, but a live session in every
agent client has **not** been tested. Check the client's instruction/skill
listing rather than assuming loading succeeded.

No provider-specific tool grants, MCP servers, paid models or cloud keys
are embedded in the skills. They describe a process, not an autonomous
multi-agent service. The scripts are the executable acceptance gate.

## Example requests

**Analyze a program**

> Read AGENTS.md and .agents/skills/cobol-analyze/SKILL.md. Analyze
> CBACT02C from its source/copybooks/JCL and create its missing migration
> map. Do not alter business behavior. Distinguish evidence from assumptions.

**Translate a new program**

> Read AGENTS.md and .agents/skills/cobol-port/SKILL.md. First identify
> any missing analysis/map/oracle prerequisites. Once those are ready,
> translate paragraph by paragraph, preserving file statuses and quirks.
> Prove byte parity before refactoring.

**Verify a change**

> Read AGENTS.md and .agents/skills/cobol-parity/SKILL.md. Run
> bash tools/verify.sh, diagnose failures, and report exact exit codes,
> datasets and untested gaps. Do not rewrite goldens to make tests pass.

## Shared completion criteria

```bash
bash tools/verify.sh
```

It runs agent-document checks, Python harness regressions, Bash syntax,
a warning-free build, runtime unit tests, full shipped-data parity and
CBACT04C-only synthetic parity. Success means these **tested cases** pass,
not that all COBOL features or IBM production behavior are supported.

See [the improvement audit](improvement-audit.md) for limitations and
next priorities. For fresh original-COBOL executions, use the scratch-output
recipes in the parity skill; the normal verification command never rewrites
tracked answer keys.

## Format references

- [AGENTS.md](https://agents.md/)
- [Claude Code memory/imports](https://code.claude.com/docs/en/memory)
- [Claude Code skills](https://code.claude.com/docs/en/skills)
- [Codex skills/discovery](https://developers.openai.com/codex/skills/)
- [Agent Skills specification](https://agentskills.io/specification)
