"""Harness regression tests; only Python 3 and Bash are required."""
import os
import subprocess
import tempfile
import unittest
from pathlib import Path

import compare
import stage

ROOT = Path(__file__).resolve().parents[2]


class ComparisonTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.run = Path(self.temp.name) / "run"
        self.golden = Path(self.temp.name) / "golden"

    def invoke(self, *programs):
        args = ["python3", str(ROOT / "tests/parity/compare.py"),
                str(self.run), str(self.golden)]
        for program in programs:
            args += ["--program", program]
        return subprocess.run(args, capture_output=True, text=True)

    def seed(self, program):
        (self.run / program).mkdir(parents=True)
        (self.golden / program).mkdir(parents=True)
        for name in compare.EXPECTED[program]:
            data = b"record\n" if name != "TRANSACT" else self.transaction()
            golden_name = "acctdata.bin" if name == "acctdata" else name
            (self.run / program / name).write_bytes(data)
            (self.golden / program / golden_name).write_bytes(data)

    @staticmethod
    def transaction(timestamp=b"2022-07-18-00.00.00.000000"):
        record = bytearray(b" " * compare.LRECL)
        for offset, length in compare.TS_SPANS:
            record[offset:offset + length] = timestamp
        return bytes(record)

    def test_missing_golden_tree_fails(self):
        result = self.invoke()
        self.assertEqual(1, result.returncode)
        self.assertEqual(4, result.stdout.count("golden directory: MISSING"))

    def test_default_requires_all_programs(self):
        self.seed("cbact04c")
        self.assertEqual(1, self.invoke().returncode)

    def test_explicit_subset_passes(self):
        self.seed("cbact04c")
        result = self.invoke("cbact04c")
        self.assertEqual(0, result.returncode, result.stdout + result.stderr)
        self.assertNotIn("cbact01c:", result.stdout)

    def test_repeated_program_selection(self):
        self.seed("cbact02c")
        self.seed("cbact03c")
        self.assertEqual(0, self.invoke("cbact02c", "cbact03c").returncode)

    def test_all_programs_pass(self):
        for program in compare.EXPECTED:
            self.seed(program)
        self.assertEqual(0, self.invoke().returncode)

    def test_missing_golden_artifact_fails_cleanly(self):
        self.seed("cbact04c")
        (self.golden / "cbact04c/acctdata.bin").unlink()
        result = self.invoke("cbact04c")
        self.assertEqual(1, result.returncode)
        self.assertIn("acctdata: MISSING in golden dir", result.stdout)
        self.assertNotIn("Traceback", result.stderr)

    def test_missing_actual_artifact_fails(self):
        self.seed("cbact02c")
        (self.run / "cbact02c/sysout.txt").unlink()
        self.assertEqual(1, self.invoke("cbact02c").returncode)

    def test_actual_directory_is_not_an_artifact(self):
        self.seed("cbact02c")
        path = self.run / "cbact02c/sysout.txt"
        path.unlink()
        path.mkdir()
        result = self.invoke("cbact02c")
        self.assertEqual(1, result.returncode)
        self.assertNotIn("Traceback", result.stderr)

    def test_first_differing_byte_is_reported(self):
        self.seed("cbact02c")
        (self.run / "cbact02c/sysout.txt").write_bytes(b"Record\n")
        result = self.invoke("cbact02c")
        self.assertEqual(1, result.returncode)
        self.assertIn("first differing byte: 0", result.stdout)

    def test_timestamp_only_difference_passes(self):
        self.seed("cbact04c")
        (self.run / "cbact04c/TRANSACT").write_bytes(
            self.transaction(b"2026-10-10-20.00.00.123456"))
        self.assertEqual(0, self.invoke("cbact04c").returncode)

    def test_invalid_timestamp_on_either_side_fails(self):
        self.seed("cbact04c")
        for side in (self.run, self.golden):
            with self.subTest(side=side):
                path = side / "cbact04c/TRANSACT"
                original = path.read_bytes()
                path.write_bytes(self.transaction(b"x" * 26))
                result = self.invoke("cbact04c")
                self.assertNotEqual(0, result.returncode)
                self.assertIn("bad timestamp format", result.stderr)
                path.write_bytes(original)

    def test_invalid_record_length_fails(self):
        self.seed("cbact04c")
        (self.run / "cbact04c/TRANSACT").write_bytes(b"short")
        self.assertNotEqual(0, self.invoke("cbact04c").returncode)

    def test_different_record_count_fails(self):
        self.seed("cbact04c")
        (self.run / "cbact04c/TRANSACT").write_bytes(self.transaction() * 2)
        self.assertEqual(1, self.invoke("cbact04c").returncode)

    def test_non_timestamp_difference_fails(self):
        self.seed("cbact04c")
        data = bytearray(self.transaction())
        data[277] = ord("X")
        (self.run / "cbact04c/TRANSACT").write_bytes(data)
        self.assertEqual(1, self.invoke("cbact04c").returncode)

    def test_unknown_program_is_rejected(self):
        self.assertEqual(2, self.invoke("not-a-program").returncode)


class StagingTests(unittest.TestCase):
    def test_crlf_records_are_space_padded(self):
        with tempfile.TemporaryDirectory() as directory:
            src, dst = Path(directory) / "src", Path(directory) / "dst"
            src.mkdir()
            (src / "cardxref.txt").write_bytes(b"one\r\ntwo\r\n")
            stage.stage(src, dst)
            self.assertEqual(b"one".ljust(50) + b"two".ljust(50),
                             (dst / "cardxref").read_bytes())

    def test_overlong_record_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            src, dst = Path(directory) / "src", Path(directory) / "dst"
            src.mkdir()
            (src / "cardxref.txt").write_bytes(b"x" * 51)
            with self.assertRaises(SystemExit):
                stage.stage(src, dst)


class RunnerTests(unittest.TestCase):
    def test_oracle_programs_do_not_share_rewritten_accounts(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            scripts = {
                "cobc": (
                    "while [ \"$1\" != -o ]; do shift; done\n"
                    "cp \"$STUB_DRIVER\" \"$2\"\n"
                    "chmod +x \"$2\"\n"
                ),
                "driver": (
                    "printf changed > \"$DD_ACCTFILE\"\n"
                    ": > \"$DD_TRANSACT\"\n"
                    "printf 'interest run\\n'\n"
                ),
                "cobcrun": (
                    "case \"$1\" in\n"
                    "LOADACCT) cp \"$FLATIN\" \"$DD_ACCTFILE\" ;;\n"
                    "LOADXREF) cp \"$FLATIN\" \"$DD_XREFFILE\" ;;\n"
                    "LOADDISC) cp \"$FLATIN\" \"$DD_DISCGRP\" ;;\n"
                    "LOADTCAT) cp \"$FLATIN\" \"$DD_TCATBALF\" ;;\n"
                    "DUMPACCT) cp \"$DD_ACCTFILE\" \"$FLATOUT\" ;;\n"
                    "CBACT01C)\n"
                    "  cat \"$DD_ACCTFILE\"\n"
                    "  cp \"$DD_ACCTFILE\" \"$DD_OUTFILE\"\n"
                    "  cp \"$DD_ACCTFILE\" \"$DD_ARRYFILE\"\n"
                    "  cp \"$DD_ACCTFILE\" \"$DD_VBRCFILE\" ;;\n"
                    "*) exit 99 ;;\n"
                    "esac\n"
                ),
            }
            for name, body in scripts.items():
                path = root / name
                path.write_text("#!/usr/bin/env bash\nset -eu\n" + body)
                path.chmod(0o755)
            data = ROOT / "tests/parity/synthetic/data"
            result = subprocess.run(
                ["bash", str(ROOT / "tests/parity/run_cobol.sh"),
                 "2022071800", str(data), str(root / "golden"),
                 "CBACT04C", "CBACT01C"],
                env=os.environ | {"PATH": str(root) + os.pathsep + os.environ["PATH"],
                                  "STUB_DRIVER": str(root / "driver"),
                                  "COB_DIR": str(root / "modules"),
                                  "VSAM_DIR": str(root / "vsam")},
                capture_output=True, text=True,
            )
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertEqual(b"changed", (root / "golden/cbact04c/acctdata.bin").read_bytes())
            self.assertEqual((data / "acctdata.txt").read_bytes(),
                             (root / "golden/cbact01c/OUTFILE").read_bytes())
            self.assertEqual(2, len(list((root / "vsam").iterdir())))

    def test_failed_process_cannot_pass_with_matching_output(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            golden = root / "golden/cbact02c"
            golden.mkdir(parents=True)
            (golden / "sysout.txt").write_bytes(b"matching output\n")
            stub = root / "dotnet"
            stub.write_text(
                "#!/usr/bin/env bash\n"
                "if [ \"$1\" = build ]; then exit 0; fi\n"
                "printf 'matching output\\n'\n"
                "exit 16\n")
            stub.chmod(0o755)
            result = subprocess.run(
                ["bash", str(ROOT / "tests/parity/run_parity.sh"),
                 "2022071800", "mainframe/data", str(root / "golden"), "CBACT02C"],
                env=os.environ | {"PATH": str(root) + os.pathsep + os.environ["PATH"],
                                  "TMPDIR": str(root)},
                capture_output=True, text=True,
            )
            self.assertEqual(1, result.returncode)
            self.assertIn("sysout.txt: identical", result.stdout)
            self.assertIn("CBACT02C exit: 16", result.stderr)

    def test_runners_reject_unknown_program_before_execution(self):
        for runner in ("run_parity.sh", "run_cobol.sh"):
            with self.subTest(runner=runner):
                result = subprocess.run(
                    ["bash", str(ROOT / "tests/parity" / runner),
                     "2022071800", "mainframe/data", "unused", "UNKNOWN"],
                    capture_output=True, text=True,
                )
                self.assertEqual(2, result.returncode)
                self.assertIn("unknown program", result.stderr)


if __name__ == "__main__":
    unittest.main()
