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
> Markdown, Mermaid diagrams, and field maps under `migration-output/docs/`
> and `migration-output/maps/`. Do not
> translate yet. List missing dependencies and open questions.

**Document and translate**

> Use cobol-to-csharp on the selected program in `legacy/batch/`. Document
> it first, then generate runnable C# .cs files under `migration-output/csharp/`.
> Put all other generated artifacts under `migration-output/` too.
> Leave the original source and data unchanged. Preserve
> file statuses, numeric behavior, and observable quirks. Compare with the
> original runtime using disposable input copies. Do not claim parity when
> the original cannot run.

**Validate an existing translation**

> Use cobol-to-csharp to verify `migration-output/csharp/` against `legacy/`.
> Report exact commands, datasets, artifact differences, masks and missing
> evidence. Do not change original sources or regenerate expected output to
> hide a mismatch.

## What the result looks like

Original code and inputs (for example, `legacy/` and `data/`) stay where they
are, unchanged.
All derived artifacts go under a new `migration-output/` root. The installed
skill's [one-page artifact checklist](../skills/cobol-to-csharp/checklists/output-artifacts.md)
contains the canonical illustrative layout and handoff audit, including which
artifacts are conditional. It is not captured migration output.

Choose a new output root that neither contains nor sits inside the source/data
trees, including through symlinks. Do not add converted files to `data/`,
edit the original README or scatter oracle binaries alongside the sources.
Existing target edits require an explicit request. Installation may add agent
configuration directories; that is separate from migration output.

Your subdirectory names may differ. A real migration handoff should contain actual
captured output from the generated project's build/run/comparison commands
when those commands can execute. This package cannot show genuine translated
output for your project without its source and an agent run. The public-source
walkthrough below supplies captured evidence for only its named synthetic case.

Common outcomes:

- **Verified for named cases:** original and target executed and compared.
- **Unverified:** evidence is missing/limited, or malformed/undefined data prevents
  a defensible equivalence claim; an expected mismatch is not a pass.
- **Blocked:** missing dependencies/tools or source compile defects prevent a step.
- **Failed:** build/tests/comparison found a concrete mismatch.

The skill tells the agent to report the difference or missing dependency,
not quietly skip it or pretend a synthetic expected output is an oracle.
Every requested program/case stays in the summary. For example, the user's
reported course run counted 18 verified, 1 unverified and 5 blocked out of 24;
that is partial named-case coverage, not "ALL PASS". That report is not an
independent reproduction by this repository's package tests.

The packaged [GnuCOBOL notes](../skills/cobol-to-csharp/references/gnucobol-semantics.md)
explain byte-level probes and their limits. The
[harness patterns](../skills/cobol-to-csharp/references/harness-patterns.md) cover
bounded processes, isolated files and honest verdicts. A
[CBL0001 walkthrough](../skills/cobol-to-csharp/references/worked-example.md)
shows the documentation/translation/validation path without bundling a demo.

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
