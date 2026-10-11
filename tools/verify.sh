#!/usr/bin/env bash
# Check the skill package only; never runs a user's migration.
set -euo pipefail
cd "$(dirname "$0")/.."

command -v python3 >/dev/null || { echo "missing tool: python3" >&2; exit 2; }
bash -n tools/verify.sh
python3 -m py_compile tests/test_skill_package.py
python3 -m unittest discover -s tests -p 'test_*.py' -v
