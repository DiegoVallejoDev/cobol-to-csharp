#!/usr/bin/env bash
# Regenerate the golden outputs by running the ORIGINAL CBACT programs under
# GnuCOBOL (requires gnucobol3 built with the BDB indexed handler).
# This is the answer key the parity test compares the C# port against.
set -euo pipefail
cd "$(dirname "$0")/../.."

PARM="${1:-2022071800}"
DATA_DIR="${2:-mainframe/data}"
GOLDEN="${3:-tests/parity/golden}"
PROGRAMS=("${@:4}")
if [ "${#PROGRAMS[@]}" -eq 0 ]; then
    PROGRAMS=(CBACT01C CBACT02C CBACT03C CBACT04C)
fi
for p in "${PROGRAMS[@]}"; do
    case "$p" in
        CBACT01C|CBACT02C|CBACT03C|CBACT04C) ;;
        *) echo "unknown program: $p" >&2; exit 2 ;;
    esac
done
COB_DIR="${COB_DIR:-$(mktemp -d "${TMPDIR:-/tmp}/cbact-cob.XXXXXX")}"
VSAM_DIR="${VSAM_DIR:-$(mktemp -d "${TMPDIR:-/tmp}/cbact-vsam.XXXXXX")}"
mkdir -p "$COB_DIR" "$VSAM_DIR" "$GOLDEN"
trap 'echo "COBOL modules: $COB_DIR"; echo "COBOL run files: $VSAM_DIR"' EXIT

# Compile loaders + target programs + stubs + PARM-driving JCL emulator
for p in LOADACCT LOADCARD LOADXREF LOADDISC LOADTCAT DUMPACCT CEE3ABD COBDATFT; do
    cobc -m -std=ibm -fsign=EBCDIC -I mainframe/cpy \
        -o "$COB_DIR/$p.so" "tools/gnucobol/$p.cbl"
done
for p in "${PROGRAMS[@]}"; do
    cobc -m -std=ibm -fsign=EBCDIC -I mainframe/cpy \
        -o "$COB_DIR/$p.so" "mainframe/cbl/$p.cbl"
done
cobc -x -std=ibm -fsign=EBCDIC -o "$COB_DIR/jcldrv" tools/gnucobol/JCLDRV.cbl

export COB_LIBRARY_PATH="$COB_DIR"

# Each oracle program receives fresh indexed inputs, just like the C# run.
for p in "${PROGRAMS[@]}"; do
    lower="${p,,}"
    work="$(mktemp -d "$VSAM_DIR/$lower.XXXXXX")"
    out="$GOLDEN/$lower"
    mkdir -p "$out"
    case "$p" in
        CBACT01C)
            FLATIN="$DATA_DIR/acctdata.txt" DD_ACCTFILE="$work/ACCTFILE" cobcrun LOADACCT
            DD_ACCTFILE="$work/ACCTFILE" DD_OUTFILE="$work/OUTFILE" \
            DD_ARRYFILE="$work/ARRYFILE" DD_VBRCFILE="$work/VBRCFILE" \
                cobcrun CBACT01C > "$out/sysout.txt"
            cp "$work"/{OUTFILE,ARRYFILE,VBRCFILE} "$out/"
            ;;
        CBACT02C)
            FLATIN="$DATA_DIR/carddata.txt" DD_CARDFILE="$work/CARDFILE" cobcrun LOADCARD
            DD_CARDFILE="$work/CARDFILE" cobcrun CBACT02C > "$out/sysout.txt"
            ;;
        CBACT03C)
            FLATIN="$DATA_DIR/cardxref.txt" DD_XREFFILE="$work/XREFFILE" cobcrun LOADXREF
            DD_XREFFILE="$work/XREFFILE" cobcrun CBACT03C > "$out/sysout.txt"
            ;;
        CBACT04C)
            FLATIN="$DATA_DIR/acctdata.txt" DD_ACCTFILE="$work/ACCTFILE" cobcrun LOADACCT
            FLATIN="$DATA_DIR/cardxref.txt" DD_XREFFILE="$work/XREFFILE" cobcrun LOADXREF
            FLATIN="$DATA_DIR/discgrp.txt" DD_DISCGRP="$work/DISCGRP" cobcrun LOADDISC
            FLATIN="$DATA_DIR/tcatbal.txt" DD_TCATBALF="$work/TCATBALF" cobcrun LOADTCAT
            DD_TCATBALF="$work/TCATBALF" DD_XREFFILE="$work/XREFFILE" \
            DD_ACCTFILE="$work/ACCTFILE" DD_DISCGRP="$work/DISCGRP" \
            DD_TRANSACT="$work/TRANSACT" \
                "$COB_DIR/jcldrv" "$PARM" > "$out/sysout.txt"
            cp "$work/TRANSACT" "$out/TRANSACT"
            DD_ACCTFILE="$work/ACCTFILE" FLATOUT="$out/acctdata.bin" cobcrun DUMPACCT
            ;;
    esac
done

echo "golden refreshed in $GOLDEN"
