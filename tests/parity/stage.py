#!/usr/bin/env python3
"""Stage CardDemo line-sequential .txt data as fixed-length records.

The shipped sample data is LF-delimited text; COBOL LINE SEQUENTIAL reads
space-pad short lines to the FD record length. The ported programs read raw
fixed-length records, so this staging pads each line to its LRECL.
"""
import sys
from pathlib import Path

LRECL = {"acctdata": 300, "carddata": 150, "cardxref": 50, "discgrp": 50, "tcatbal": 50}


def stage(src_dir: Path, dst_dir: Path) -> None:
    dst_dir.mkdir(parents=True, exist_ok=True)
    for name, lrecl in LRECL.items():
        if not (src_dir / f"{name}.txt").exists():
            continue
        out = bytearray()
        with open(src_dir / f"{name}.txt", "rb") as f:
            for line in f.read().split(b"\n"):
                line = line.rstrip(b"\r")
                if not line:
                    continue
                if len(line) > lrecl:
                    sys.exit(f"{name}: line longer than LRECL {lrecl}")
                out += line.ljust(lrecl, b" ")
        (dst_dir / name).write_bytes(out)
        print(f"{name}: {len(out) // lrecl} records")


if __name__ == "__main__":
    stage(Path(sys.argv[1]), Path(sys.argv[2]))
