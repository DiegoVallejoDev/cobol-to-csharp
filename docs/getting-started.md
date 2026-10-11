# Install the skill and use it on your COBOL

## What you are installing

You are installing instructions for your existing coding agent, not a converter
binary or a sample C# application. The agent reads your COBOL, documents it,
and writes C# and tests into **your project**. There is no bundled COBOL estate,
target runtime library, or model/API service.

You need a file-capable coding agent and your source/copybooks/JCL. Install .NET
in the target development environment to build generated `.cs` projects. To
prove behavioral parity, you also need a compatible original COBOL runtime
and safe inputs; analysis itself does not require either compiler.

## Option 1: skills CLI

From your target project's root:

```bash
npx skills add DiegoVallejoDev/cobol-to-csharp --skill cobol-to-csharp
```

This optional installer requires a supported Node.js version/network access.
Select the agent and
installation method interactively. To explicitly select Claude Code or Codex:

```bash
npx skills add DiegoVallejoDev/cobol-to-csharp --skill cobol-to-csharp --agent claude-code
npx skills add DiegoVallejoDev/cobol-to-csharp --skill cobol-to-csharp --agent codex
```

Inspect the installed skill listing in your client. Loader support varies by
version. These commands target the repository's default branch; use the local
copy method below when evaluating an unmerged branch.

## Option 2: copy the self-contained directory

Clone this skill repository somewhere **outside** the target source project:

```bash
git clone https://github.com/DiegoVallejoDev/cobol-to-csharp.git "$HOME/cobol-to-csharp-skill"
```

From your target project's root, choose one destination:

```bash
# Claude Code project skill
mkdir -p .claude/skills
cp -R "$HOME/cobol-to-csharp-skill/skills/cobol-to-csharp" .claude/skills/

# Codex / clients supporting .agents/skills
mkdir -p .agents/skills
cp -R "$HOME/cobol-to-csharp-skill/skills/cobol-to-csharp" .agents/skills/
```

Do not copy only SKILL.md: it links to packaged references and templates.
Inspect any existing destination first; do not overwrite an installed skill
without deciding whether to update it. Consumer installation needs neither
this repository's tests nor its root AGENTS.md/CLAUDE.md.

## Option 3: Claude plugin or explicit file loading

The `.claude-plugin/plugin.json` manifest points to the same canonical skill.
For a local Claude Code session in the **target project**, use:

```bash
claude --plugin-dir "$HOME/cobol-to-csharp-skill"
```

Claude plugins namespace their skills; use `/cobol-to-csharp:cobol-to-csharp`.
A manually installed project skill is invoked as `/cobol-to-csharp`.
In Codex, mention `$cobol-to-csharp` after it appears in the skill listing.

For Devin or any other file-capable agent, provide the source workspace and
the path to `skills/cobol-to-csharp/SKILL.md` in your checkout. Ask it to read
that file and follow its linked references. Native plugin/skill installation
is client-specific; explicit loading is the fallback, not a promise that every
client automatically discovers this repository.

## Give the agent a bounded request

Replace paths/program names with your own. No named sample is required.

**Documentation only**

> Use cobol-to-csharp. Analyze the programs under `legacy/batch/`, copybooks
> under `legacy/copybooks/`, and JCL under `legacy/jobs/`. Write source-backed
> Markdown, Mermaid diagrams, and field maps under `migration/`. Do not
> translate yet. List missing dependencies and open questions.

**Document and translate**

> Use cobol-to-csharp on the selected program in `legacy/batch/`. Document
> it first, then generate runnable C# .cs files under `modernized/`. Preserve
> file statuses, numeric behavior, and observable quirks. Compare with the
> original runtime using disposable input copies. Do not claim parity when
> the original cannot run.

**Validate an existing translation**

> Use cobol-to-csharp to verify `modernized/` against the original in `legacy/`.
> Report exact commands, datasets, artifact differences, masks and missing
> evidence. Do not change original sources or regenerate expected output to
> hide a mismatch.

## What the result looks like

The following is an **illustrative layout**, not captured migration output:

```text
your-project/
  legacy/                         # Your original code, unchanged
  migration/
    analysis/<PROGRAM>.md         # Dependencies, data, flow, errors
    maps/<PROGRAM>.json           # Byte ranges, rules, traceability
    validation.md                 # Commands, results, gaps
    getting-started.md             # How to run the generated target
  modernized/
    ... .cs / project files       # Structure chosen for your project
    ... tests / IO adapters       # Only what your source requires
```

Your layout may differ. A real migration handoff should contain actual
captured output from the generated project's build/run/comparison commands
when those commands can execute. This package cannot show genuine translated
output without your source project and an agent run.

Common outcomes:

- **Verified for named cases:** original and target executed and compared.
- **Unverified:** C# was produced, but original-runtime evidence is missing.
- **Blocked:** missing copybooks, service contracts, or tools prevent a step.
- **Failed:** build/tests/comparison found a concrete mismatch.

The skill tells the agent to report the difference or missing dependency,
not quietly skip it or pretend a synthetic expected output is an oracle.

## Check the package itself

From this repository's root:

```bash
bash tools/verify.sh
```

Requires Python 3 and Bash. It validates packaging and templates and tests
standalone installation in an isolated directory. It does not invoke an LLM,
translate a program, or validate generated C#.

Actual package-check output (timing omitted):

```text
Ran 12 tests

OK
```

The skills CLI (1.7.2) also discovered exactly one skill and successfully
copied the complete package into isolated Claude Code and Codex project paths.
The installed skill was discoverable through Devin's skill search. Live
Claude/Codex agent sessions and a real-source translation were not run; package
installation is not evidence of translation quality.

## Format and client references

- [Agent Skills format](https://agentskills.io/specification)
- [Skills CLI installation](https://github.com/vercel-labs/skills)
- [Claude Code skills and plugins](https://code.claude.com/docs/en/skills)
- [Codex skills](https://developers.openai.com/codex/skills/)
