#!/usr/bin/env python3
"""Generate a non-degenerate synthetic dataset exercising every branch of
CBACT04C: nonzero interest, multi-category accounts, DEFAULT group fallback,
rate=0 skip, REWRITE of cyc fields, and the dead-ELSE last-account quirk.
Writes LF-delimited .txt files (COBOL LINE SEQUENTIAL format).
"""
import sys
from pathlib import Path


def zoned(value_cents: int, width: int) -> bytes:
    """Encode s9(n)v99 as zoned decimal with EBCDIC overpunch sign."""
    neg = value_cents < 0
    digits = f"{abs(value_cents):0{width}d}"[-width:]
    last = int(digits[-1])
    tail = "}" if neg and last == 0 else ("J" if neg else "{") if last == 0 else chr(ord("J") + last - 1 if neg else ord("A") + last - 1)
    return (digits[:-1] + tail).encode()


def acct(acct_id: str, curr_bal: int, cyc_cred: int, cyc_deb: int, grp: str) -> bytes:
    r = bytearray(b" " * 300)
    r[0:11] = acct_id.encode()
    r[11:12] = b"Y"                       # status
    r[12:24] = zoned(curr_bal, 12)        # ACCT-CURR-BAL s9(10)v99
    r[24:36] = zoned(0, 12)               # credit limit
    r[36:48] = zoned(0, 12)               # cash limit
    r[48:58] = b"2020-01-01"              # open date
    r[58:68] = b"2030-01-01"              # expiry
    r[68:78] = b"2030-01-01"              # reissue
    r[78:90] = zoned(cyc_cred, 12)        # cyc credit
    r[90:102] = zoned(cyc_deb, 12)        # cyc debit
    r[102:112] = b"00000-0000"
    r[112:122] = grp.encode().ljust(10)   # ACCT-GROUP-ID
    return bytes(r)


def tcat(acct_id: str, typ: str, cat: str, bal: int) -> bytes:
    r = bytearray(b" " * 50)
    r[0:11] = acct_id.encode()
    r[11:13] = typ.encode()
    r[13:17] = cat.encode()
    r[17:28] = zoned(bal, 11)             # TRAN-CAT-BAL s9(9)v99
    return bytes(r)


def xref(card: str, cust: str, acct_id: str) -> bytes:
    r = bytearray(b" " * 50)
    r[0:16] = card.encode()
    r[16:25] = cust.encode()
    r[25:36] = acct_id.encode()
    return bytes(r)


def disc(grp: str, typ: str, cat: str, rate: int) -> bytes:
    r = bytearray(b" " * 50)
    r[0:10] = grp.encode().ljust(10)
    r[10:12] = typ.encode()
    r[12:16] = cat.encode()
    r[16:22] = zoned(rate, 6)             # DIS-INT-RATE s9(4)v99
    return bytes(r)


def main() -> None:
    out = Path(sys.argv[1])
    out.mkdir(parents=True, exist_ok=True)

    # a1: G1, two categories -> interest rows; a2: G2 rate 0 -> skipped;
    # a3: no group match -> DEFAULT rate, and is LAST account (dead-ELSE)
    (out / "acctdata.txt").write_bytes(b"\n".join([
        acct("00000000001", 10000, 500, 0, "G1"),     # 100.00, cyc 5.00/0
        acct("00000000002", 20000, 700, 800, "G2"),   # 200.00, cyc 7.00/8.00
        acct("00000000003", 30000, 900, 1100, "XX"),  # 300.00, cyc 9.00/11.00
    ]) + b"\n")
    (out / "tcatbal.txt").write_bytes(b"\n".join([
        tcat("00000000001", "01", "0001", 120000),    # 1200.00 @ 12.00%
        tcat("00000000001", "01", "0002", 60000),     #  600.00 @  6.00%
        tcat("00000000002", "01", "0001", 90000),     #  900.00 @  0.00%
        tcat("00000000003", "09", "0009", 10000),     #  100.00 @ 24.50% (DEFAULT)
    ]) + b"\n")
    (out / "cardxref.txt").write_bytes(b"\n".join([
        xref("4000111122223333", "000000001", "00000000001"),
        xref("4000111122224444", "000000002", "00000000002"),
        xref("4000111122225555", "000000003", "00000000003"),
    ]) + b"\n")
    (out / "discgrp.txt").write_bytes(b"\n".join([
        disc("G1", "01", "0001", 1200),               # 12.00%
        disc("G1", "01", "0002", 600),                #  6.00%
        disc("G2", "01", "0001", 0),                  #  0.00% -> row skipped
        disc("DEFAULT", "09", "0009", 2450),          # 24.50%
    ]) + b"\n")

    # Expected results (for the record):
    # a1 int = 1200*12/1200 + 600*6/1200 = 12.00 + 3.00 = 15.00
    #   -> TRANSACT 2 rows; curr-bal 115.00; cyc -> 0/0
    # a2: rate 0 -> no row; total 0 -> curr-bal stays 200.00; cyc -> 0/0
    # a3: DEFAULT 24.50% -> int trunc(100*24.5/1200)=2.04 -> 1 row
    #   -> DEAD-ELSE: last account never flushed; curr-bal stays 300.00, cyc 9.00/11.00
    for name in ("acctdata", "tcatbal", "cardxref", "discgrp"):
        print(name, "->", (out / f"{name}.txt").stat().st_size, "bytes")


if __name__ == "__main__":
    main()
