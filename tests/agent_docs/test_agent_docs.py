"""Lightweight checks for the repository's agent adapters and skill contracts."""
import re
import unittest
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[2]
SKILLS = ("cobol-analyze", "cobol-port", "cobol-parity")


class AgentDocumentationTests(unittest.TestCase):
    def test_claude_imports_canonical_rules(self):
        text = (ROOT / "CLAUDE.md").read_text()
        self.assertRegex(text, r"(?m)^@AGENTS\.md$")
        self.assertTrue((ROOT / "AGENTS.md").is_file())

    def test_skills_have_standard_metadata(self):
        for directory in (".agents/skills", ".claude/skills"):
            for name in SKILLS:
                with self.subTest(directory=directory, name=name):
                    path = ROOT / directory / name / "SKILL.md"
                    text = path.read_text()
                    metadata = re.match(r"\A---\n(.*?)\n---\n", text, re.S)
                    self.assertIsNotNone(metadata, str(path))
                    fields = dict(line.split(": ", 1)
                                  for line in metadata[1].splitlines())
                    self.assertEqual(name, fields["name"])
                    self.assertRegex(fields["name"], r"^[a-z0-9]+(-[a-z0-9]+)*$")
                    self.assertLessEqual(len(fields["name"]), 64)
                    self.assertTrue(0 < len(fields["description"]) <= 1024)
                    self.assertIn("AGENTS.md", text[metadata.end():])

    def test_claude_adapters_point_to_canonical_skills(self):
        for name in SKILLS:
            with self.subTest(name=name):
                target = f".agents/skills/{name}/SKILL.md"
                text = (ROOT / ".claude/skills" / name / "SKILL.md").read_text()
                self.assertIn(target, text)
                self.assertTrue((ROOT / target).is_file())

    def test_document_local_links_resolve(self):
        for relative in ("README.md", "docs/agents.md", "docs/improvement-audit.md"):
            path = ROOT / relative
            for target in re.findall(r"\[[^\]]*\]\(([^)]+)\)", path.read_text()):
                if "://" in target or target.startswith("#"):
                    continue
                with self.subTest(path=relative, target=target):
                    local = target.split("#", 1)[0]
                    self.assertTrue((path.parent / unquote(local)).exists(), target)


if __name__ == "__main__":
    unittest.main()
