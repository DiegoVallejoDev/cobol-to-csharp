#!/usr/bin/env python3
"""Compare C# port outputs against the GnuCOBOL golden outputs.

- TRANSACT: byte-compare each 350-byte record EXCLUDING the two timestamp
  fields (offsets 278 and 304, 26 bytes each) — those carry the wall clock
  and are compared structurally (format YYYY-MM-DD-HH.MM.SS.ff0000) instead.
- sysout / OUTFILE / ARRYFILE / VBRCFILE / acctdata: exact byte compare.

Usage: compare.py <run-dir> <golden-dir>
The golden dir holds one subdirectory per program (cbact01c, cbact02c, ...).
"""
import re
import sys
from pathlib import Path

TS_RE = re.compile(rb"^\d{4}-\d{2}-\d{2}-\d{2}\.\d{2}\.\d{2}\.\d{6}$")
TS_SPANS = [(278, 26), (304, 26)]
LRECL = 350

# program -> outputs to compare
EXPECTED = {
    "cbact01c": ["sysout.txt", "OUTFILE", "ARRYFILE", "VBRCFILE"],
    "cbact02c": ["sysout.txt"],
    "cbact03c": ["sysout.txt"],
    "cbact04c": ["sysout.txt", "TRANSACT", "acctdata"],
}


def masked_records(path: Path):
    data = path.read_bytes()
    if len(data) % LRECL:
        sys.exit(f"{path}: size {len(data)} not a multiple of {LRECL}")
    for i in range(0, len(data), LRECL):
        rec = bytearray(data[i : i + LRECL])
        for off, n in TS_SPANS:
            ts = bytes(rec[off : off + n])
            if not TS_RE.match(ts):
                sys.exit(f"{path}: bad timestamp format {ts!r} in record {i // LRECL}")
            rec[off : off + n] = b"T" * n
        yield bytes(rec)


def compare_transact(actual: Path, golden: Path) -> bool:
    a, g = list(masked_records(actual)), list(masked_records(golden))
    if len(a) != len(g):
        print(f"  TRANSACT: record count differs ({len(a)} vs {len(g)})")
        return False
    diffs = [i for i, (r1, r2) in enumerate(zip(a, g)) if r1 != r2]
    if diffs:
        for i in diffs[:5]:
            print(f"  TRANSACT: record {i} differs")
        return False
    print(f"  TRANSACT: {len(a)} records identical (timestamps masked + format-checked)")
    return True


def compare_exact(actual: Path, golden: Path, label: str) -> bool:
    a, g = actual.read_bytes(), golden.read_bytes()
    if a == g:
        print(f"  {label}: identical ({len(a)} bytes)")
        return True
    print(f"  {label}: DIFFERS ({len(a)} vs {len(g)} bytes)")
    n = min(len(a), len(g))
    first = next((i for i in range(n) if a[i] != g[i]), n)
    print(f"    first differing byte: {first}")
    la = a.split(b"\n"); lg = g.split(b"\n")
    for i, (x, y) in enumerate(zip(la, lg)):
        if x != y:
            print(f"    line {i}: {x[:80]!r} != {y[:80]!r}")
            break
    return False


def main() -> int:
    run_dir, golden_dir = Path(sys.argv[1]), Path(sys.argv[2])
    ok = True
    for prog, outputs in EXPECTED.items():
        gdir = golden_dir / prog
        if not gdir.is_dir():
            continue
        print(f"{prog}:")
        for name in outputs:
            actual = run_dir / prog / name
            if not actual.exists():
                print(f"  {name}: MISSING in run dir")
                ok = False
                continue
            golden = gdir / (name if name != "acctdata" else "acctdata.bin")
            if name == "TRANSACT":
                ok &= compare_transact(actual, golden)
            else:
                ok &= compare_exact(actual, golden, name)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
