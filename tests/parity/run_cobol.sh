#!/usr/bin/env bash
# Regenerate the golden outputs by running the ORIGINAL CBACT04C under
# GnuCOBOL (requires gnucobol3 built with the BDB indexed handler).
# This is the answer key the parity test compares the C# port against.
set -euo pipefail
cd "$(dirname "$0")/../.."

COB_DIR="${COB_DIR:-/tmp/cob}"
VSAM_DIR="${VSAM_DIR:-/tmp/vsam}"
PARM="${1:-2022071800}"
DATA_DIR="${2:-mainframe/data}"
GOLDEN="${3:-tests/parity/golden}"
mkdir -p "$COB_DIR" "$VSAM_DIR" "$GOLDEN"

# Compile loaders + target programs + stubs + PARM-driving JCL emulator
for p in LOADACCT LOADCARD LOADXREF LOADDISC LOADTCAT DUMPACCT CEE3ABD COBDATFT; do
    cobc -m -std=ibm -fsign=EBCDIC -I mainframe/cpy \
        -o "$COB_DIR/$p.so" "tools/gnucobol/$p.cbl"
done
for p in CBACT01C CBACT02C CBACT03C CBACT04C; do
    cobc -m -std=ibm -fsign=EBCDIC -I mainframe/cpy \
        -o "$COB_DIR/$p.so" "mainframe/cbl/$p.cbl"
done
cobc -x -std=ibm -fsign=EBCDIC -o "$COB_DIR/jcldrv" tools/gnucobol/JCLDRV.cbl

export COB_LIBRARY_PATH="$COB_DIR"

# Stage flat data into indexed (VSAM-equivalent) files
rm -f "$VSAM_DIR"/{ACCTFILE,CARDFILE,XREFFILE,XREFFILE.1,DISCGRP,TCATBALF,TRANSACT,OUTFILE,ARRYFILE,VBRCFILE}
FLATIN="$DATA_DIR/acctdata.txt"  DD_ACCTFILE="$VSAM_DIR/ACCTFILE" cobcrun LOADACCT
FLATIN="$DATA_DIR/carddata.txt"  DD_CARDFILE="$VSAM_DIR/CARDFILE" cobcrun LOADCARD
FLATIN="$DATA_DIR/cardxref.txt"  DD_XREFFILE="$VSAM_DIR/XREFFILE" cobcrun LOADXREF
FLATIN="$DATA_DIR/discgrp.txt"   DD_DISCGRP="$VSAM_DIR/DISCGRP"   cobcrun LOADDISC
FLATIN="$DATA_DIR/tcatbal.txt"   DD_TCATBALF="$VSAM_DIR/TCATBALF" cobcrun LOADTCAT

mkdir -p "$GOLDEN/cbact04c" "$GOLDEN/cbact01c" "$GOLDEN/cbact02c" "$GOLDEN/cbact03c"

# --- INTCALC step: EXEC PGM=CBACT04C,PARM='...'
DD_TCATBALF="$VSAM_DIR/TCATBALF" DD_XREFFILE="$VSAM_DIR/XREFFILE" \
DD_ACCTFILE="$VSAM_DIR/ACCTFILE" DD_DISCGRP="$VSAM_DIR/DISCGRP" \
DD_TRANSACT="$VSAM_DIR/TRANSACT" \
    "$COB_DIR/jcldrv" "$PARM" > "$GOLDEN/cbact04c/sysout.txt"

cp "$VSAM_DIR/TRANSACT" "$GOLDEN/cbact04c/TRANSACT"
# Dump ACCTFILE byte-exact (RECORD SEQUENTIAL, no trimming)
DD_ACCTFILE="$VSAM_DIR/ACCTFILE" FLATOUT="$GOLDEN/cbact04c/acctdata.bin" cobcrun DUMPACCT

# --- CBACT01C: acct reader -> OUTFILE / ARRYFILE / VBRCFILE
DD_ACCTFILE="$VSAM_DIR/ACCTFILE" DD_OUTFILE="$VSAM_DIR/OUTFILE" \
DD_ARRYFILE="$VSAM_DIR/ARRYFILE" DD_VBRCFILE="$VSAM_DIR/VBRCFILE" \
    cobcrun CBACT01C > "$GOLDEN/cbact01c/sysout.txt"
cp "$VSAM_DIR"/{OUTFILE,ARRYFILE,VBRCFILE} "$GOLDEN/cbact01c/"

# --- CBACT02C: card reader (sysout only)
DD_CARDFILE="$VSAM_DIR/CARDFILE" cobcrun CBACT02C > "$GOLDEN/cbact02c/sysout.txt"

# --- CBACT03C: xref reader (sysout only)
DD_XREFFILE="$VSAM_DIR/XREFFILE" cobcrun CBACT03C > "$GOLDEN/cbact03c/sysout.txt"

echo "golden refreshed in $GOLDEN"
