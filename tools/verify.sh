#!/usr/bin/env bash
# Read-only acceptance gate: never regenerates checked-in goldens.
set -euo pipefail
cd "$(dirname "$0")/.."

for tool in dotnet python3; do
    command -v "$tool" >/dev/null || { echo "missing tool: $tool" >&2; exit 2; }
done

bash -n tools/verify.sh tests/parity/run_parity.sh tests/parity/run_cobol.sh
python3 -m unittest discover -s tests/agent_docs -p 'test_*.py' -v
python3 -m unittest discover -s tests/parity -p 'test_*.py' -v
dotnet build CobolModernization.sln --nologo -warnaserror
dotnet test CobolModernization.sln --no-build --nologo
tests/parity/run_parity.sh 2022071800 mainframe/data tests/parity/golden
tests/parity/run_parity.sh 2022071800 tests/parity/synthetic/data \
    tests/parity/synthetic/golden CBACT04C
