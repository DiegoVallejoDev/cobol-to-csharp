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
PROGRAMS=("${@:4}")
if [ "${#PROGRAMS[@]}" -eq 0 ]; then
    PROGRAMS=(CBACT01C CBACT02C CBACT03C CBACT04C)
fi
for prog in "${PROGRAMS[@]}"; do
    case "$prog" in
        CBACT01C|CBACT02C|CBACT03C|CBACT04C) ;;
        *) echo "unknown program: $prog" >&2; exit 2 ;;
    esac
done
RUN_DIR="$(mktemp -d "${TMPDIR:-/tmp}/cbact-cs.XXXXXX")"
trap 'echo "run dir: $RUN_DIR"' EXIT

python3 tests/parity/stage.py "$DATA_DIR" "$RUN_DIR"
dotnet build src/CardDemo.Batch -v q --nologo

# Each program gets its own data dir so outputs don't clash.
status=0
compare_args=()
for prog in "${PROGRAMS[@]}"; do
    lower="${prog,,}"
    compare_args+=(--program "$lower")
    mkdir -p "$RUN_DIR/$lower"
    for base in acctdata carddata cardxref discgrp tcatbal; do
        [ -f "$RUN_DIR/$base" ] && cp "$RUN_DIR/$base" "$RUN_DIR/$lower/"
    done
    if dotnet run --project src/CardDemo.Batch --no-build -- "$prog" "$PARM" \
        "$RUN_DIR/$lower" > "$RUN_DIR/$lower/sysout.txt"; then
        :
    else
        code=$?
        echo "$prog exit: $code" >&2
        status=1
    fi
done

python3 tests/parity/compare.py "$RUN_DIR" "$GOLDEN" "${compare_args[@]}" || status=1
exit "$status"
