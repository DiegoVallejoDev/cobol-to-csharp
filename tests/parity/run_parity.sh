#!/usr/bin/env bash
# Golden-master parity test for the CBACT0*C ports.
# Runs each C# program on the same staged data the COBOL run used and diffs
# its outputs against tests/parity/golden (captured from GnuCOBOL —
# regenerate with run_cobol.sh).
set -euo pipefail
cd "$(dirname "$0")/../.."

PARM="${1:-2022071800}"
DATA_DIR="${2:-mainframe/data}"
GOLDEN="${3:-tests/parity/golden}"
RUN_DIR="$(mktemp -d /tmp/cbact-cs.XXXXXX)"
trap 'echo "run dir: $RUN_DIR"' EXIT

python3 tests/parity/stage.py "$DATA_DIR" "$RUN_DIR"
dotnet build src/CardDemo.Batch -v q --nologo

# Each program gets its own data dir so outputs don't clash.
for prog in CBACT01C CBACT02C CBACT03C CBACT04C; do
    lower="${prog,,}"
    mkdir -p "$RUN_DIR/$lower"
    for base in acctdata carddata cardxref discgrp tcatbal; do
        [ -f "$RUN_DIR/$base" ] && cp "$RUN_DIR/$base" "$RUN_DIR/$lower/"
    done
    dotnet run --project src/CardDemo.Batch --no-build -- "$prog" "$PARM" \
        "$RUN_DIR/$lower" > "$RUN_DIR/$lower/sysout.txt" || echo "$prog exit: $?"
done

python3 tests/parity/compare.py "$RUN_DIR" "$GOLDEN"
