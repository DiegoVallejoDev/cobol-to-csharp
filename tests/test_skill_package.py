"""Contract checks for a portable skill package, not a migration demo."""

import json
from pathlib import Path
import re
import shutil
import tempfile
import unittest
from urllib.parse import unquote, urlsplit


ROOT = Path(__file__).resolve().parents[1]
SKILL = ROOT / "skills/cobol-to-csharp"


def metadata(path):
    text = path.read_text(encoding="utf-8")
    if not text.startswith("---\n"):
        raise AssertionError(f"missing frontmatter: {path}")
    parts = text.split("---\n", 2)
    if len(parts) != 3:
        raise AssertionError(f"unclosed frontmatter: {path}")
    return dict(line.split(": ", 1) for line in parts[1].splitlines())


def check_links(test, files, boundary):
    for path in files:
        text = path.read_text(encoding="utf-8")
        for link in re.findall(r"\[[^\]]*\]\(([^)\s]+)\)", text):
            parsed = urlsplit(link)
            if parsed.scheme or parsed.netloc or not parsed.path:
                continue
            target = (path.parent / unquote(parsed.path)).resolve()
            resolved_boundary = boundary.resolve()
            test.assertTrue(
                target == resolved_boundary or resolved_boundary in target.parents,
                link,
            )
            test.assertTrue(target.exists(), f"{path}: missing {link}")


class SkillPackageTests(unittest.TestCase):
    def test_exactly_one_public_skill(self):
        self.assertEqual([SKILL / "SKILL.md"], list(ROOT.glob("skills/*/SKILL.md")))

    def test_standard_skill_metadata(self):
        fields = metadata(SKILL / "SKILL.md")
        name = fields["name"]
        self.assertEqual(SKILL.name, name)
        self.assertRegex(name, r"^[a-z0-9]+(?:-[a-z0-9]+)*$")
        self.assertLessEqual(len(name), 64)
        self.assertTrue(1 <= len(fields["description"]) <= 1024)
        self.assertIn("COBOL", fields["description"])
        self.assertIn("C#", fields["description"])
        self.assertNotIn("allowed-tools", fields)
        self.assertNotIn("model", fields)

    def test_skill_resources_are_internal_and_resolve(self):
        check_links(self, SKILL.rglob("*.md"), SKILL)

    def test_human_documentation_links_resolve(self):
        files = [ROOT / "README.md", ROOT / "AGENTS.md", ROOT / "CLAUDE.md"]
        check_links(self, [*files, *ROOT.glob("docs/*.md")], ROOT)

    def test_claude_contributor_rules_import_shared_rules(self):
        self.assertIn("@AGENTS.md", (ROOT / "CLAUDE.md").read_text())
        self.assertTrue((ROOT / "AGENTS.md").is_file())

    def test_claude_plugin_resolves_canonical_skill(self):
        manifest = json.loads(
            (ROOT / ".claude-plugin/plugin.json").read_text(encoding="utf-8")
        )
        self.assertEqual("cobol-to-csharp", manifest["name"])
        self.assertRegex(manifest["version"], r"^\d+\.\d+\.\d+$")
        self.assertEqual(ROOT / "skills", ROOT / manifest["skills"])
        entrypoint = ROOT / manifest["skills"] / SKILL.name / "SKILL.md"
        self.assertTrue(entrypoint.is_file())

    def test_standalone_copy_installation_for_both_project_paths(self):
        source_files = {
            path.relative_to(SKILL): path.read_bytes()
            for path in SKILL.rglob("*")
            if path.is_file()
        }
        for client_dir in (".agents", ".claude"):
            with self.subTest(client=client_dir):
                self.check_copy_installation(client_dir, source_files)

    def check_copy_installation(self, client_dir, source_files):
        with tempfile.TemporaryDirectory() as directory:
            project = Path(directory)
            installed = project / client_dir / "skills" / SKILL.name
            shutil.copytree(SKILL, installed)
            self.assertEqual(SKILL.name, metadata(installed / "SKILL.md")["name"])
            check_links(self, installed.rglob("*.md"), installed)
            for relative, contents in source_files.items():
                self.assertEqual(contents, (installed / relative).read_bytes())
            self.assertFalse((project / "AGENTS.md").exists())
            self.assertFalse((project / "tools").exists())
            self.assertFalse((project / "src").exists())

    def test_migration_map_is_an_unfilled_generic_contract(self):
        template = json.loads((SKILL / "assets/migration-map.json").read_text())
        self.assertEqual("1", template["schema_version"])
        self.assertIsNone(template["program"])
        self.assertIsNone(template["source"]["dialect"])
        self.assertIsNone(template["source"]["encoding"])
        self.assertIsNone(template["target"]["format"])
        record = template["records"][0]
        field = record["fields"][0]
        for key in ("length_bytes", "source_reference"):
            self.assertIsNone(record[key])
        for key in (
            "offset_bytes", "length_bytes", "scale", "usage", "pic",
            "sign", "encoding", "occurs", "redefines", "source_reference",
        ):
            self.assertIsNone(field[key])
        self.assertIn("status_branches", template["files"][0])
        self.assertIn("mutates_input", template["files"][0])
        self.assertIn("target_file_and_method", template["paragraphs"][0])
        for key in ("dependencies", "quirks", "numeric_rules", "unresolved"):
            self.assertEqual([], template[key])

    def test_report_template_requires_evidence_and_gaps(self):
        report = (SKILL / "assets/validation-report.md").read_text()
        for required in (
            "unverified", "blocked", "failed", "Exit code", "Oracle provenance",
            "input isolation", "Missing artifacts", "masks", "Gaps",
        ):
            self.assertIn(required, report)

    def test_entrypoint_covers_all_phases(self):
        text = (SKILL / "SKILL.md").read_text()
        for phase in ("Phase 1", "Phase 2", "Phase 3"):
            self.assertIn(phase, text)
        for resource in (
            "references/analysis.md", "references/translation.md",
            "references/verification.md", "references/compatibility.md",
            "assets/program-analysis.md", "assets/migration-map.json",
            "assets/validation-report.md",
        ):
            self.assertIn(resource, text)
        self.assertIn("unverified translation", text)

    def test_demo_and_runtime_are_not_present(self):
        for relative in (
            "mainframe", "src", "migration-map", "CobolModernization.sln",
            "tests/parity", "tests/Legacy.Runtime.Tests", "tools/gnucobol",
            ".devin/blueprint.yaml", ".agents", ".claude", "docs/analysis",
        ):
            self.assertFalse((ROOT / relative).exists(), relative)
        for pattern in (
            "*.cbl", "*.cpy", "*.cs", "*.csx", "*.csproj", "*.sln", "*.bin",
        ):
            self.assertEqual([], list(SKILL.rglob(pattern)), pattern)

    def test_skill_does_not_require_contributor_repository_files(self):
        for path in SKILL.rglob("*.md"):
            text = path.read_text()
            for repo_dependency in (
                "AGENTS.md", "CLAUDE.md", "tools/verify.sh",
                "tests/parity/", "src/CardDemo", "/home/ubuntu/",
            ):
                self.assertNotIn(repo_dependency, text, str(path))


if __name__ == "__main__":
    unittest.main()
