# CBACT04C — Interest Calculator (batch)

JCL: `INTCALC.jcl` → `STEP15 EXEC PGM=CBACT04C,PARM='2022071800'`. PARM is a 10-byte date string used verbatim in generated transaction IDs (`PARM-LENGTH S9(04) COMP` prefix is ignored in practice).

## Purpose

Reads the transaction-category balance file sequentially (grouped by account). For each account group, accumulates monthly interest per category record using the disclosure-group rate, writes one TRANSACT record per qualifying category record (type `01`, category `05`, source `System`), and rewrites the account master with interest added to `ACCT-CURR-BAL` and cycle credit/debit zeroed.

## Files

| DD | Mode | Org/Access | Record | Notes |
|---|---|---|---|---|
| TCATBALF | INPUT | INDEXED / SEQUENTIAL, key `FD-TRAN-CAT-KEY` (acct 9(11) + type X(2) + cat 9(4)) | `TRAN-CAT-BAL-RECORD` 50B (CVTRA01Y) | Driver file — must arrive in key order |
| XREFFILE | INPUT | INDEXED / RANDOM, key `FD-XREF-CARD-NUM`, alt key `FD-XREF-ACCT-ID` | `CARD-XREF-RECORD` 50B (CVACT03Y) | Read by alt key to get card number for TRANSACT |
| ACCTFILE | I-O | INDEXED / RANDOM, key `FD-ACCT-ID` | `ACCOUNT-RECORD` 300B (CVACT01Y) | Rewritten per account |
| DISCGRP | INPUT | INDEXED / RANDOM, key `FD-DISCGRP-KEY` (group X(10) + type X(2) + cat 9(4)) | `DIS-GROUP-RECORD` 50B (CVTRA02Y) | '23' → retry with `'DEFAULT'` group id |
| TRANSACT | OUTPUT | SEQUENTIAL, LRECL 350 | `TRAN-RECORD` 350B (CVTRA05Y) | New GDG member on mainframe |

## Working storage of note

`WS-MONTHLY-INT S9(09)V99`, `WS-TOTAL-INT S9(09)V99`, `WS-LAST-ACCT-NUM X(11)`, `WS-FIRST-TIME X(1)`, `WS-RECORD-COUNT 9(09)`, `WS-TRANID-SUFFIX 9(06)`, `END-OF-FILE`, `APPL-RESULT` with 88-levels `APPL-AOK=0` / `APPL-EOF=16`.

## Business rules

- `WS-MONTHLY-INT = (TRAN-CAT-BAL * DIS-INT-RATE) / 1200` — COMPUTE truncation (no ROUNDED); DIS-INT-RATE is annual percent, so /1200 = monthly fraction.
- Interest and a TRANSACT row are produced **only when `DIS-INT-RATE ≠ 0`**.
- TRANSACT fields: `TRAN-ID = PARM-DATE + 6-digit counter` (STRING DELIMITED BY SIZE), type `01`, cat `05`, source `System`, desc `Int. for a/c <ACCT-ID>`, amount = this record's monthly interest (not the account total), merchant fields zero/blank, `TRAN-CARD-NUM` from XREF, both timestamps = current DB2-format `YYYY-MM-DD-HH.MM.SS.NN0000`.
- Account update happens at **group boundary**: on first record of a new account, previous account's `ACCT-CURR-BAL += WS-TOTAL-INT`, cycle credit/debit → 0, REWRITE.

## Error handling

- Any open/read/write/rewrite/close status ≠ expected → DISPLAY message + `9910-DISPLAY-IO-STATUS` + `9999-ABEND-PROGRAM` (`CALL 'CEE3ABD'` code 999 → Language Environment abend).
- DISCGRP read tolerates `23` (record not found) → DISPLAY 2 lines → retry key `DEFAULT`.
- ACCT/XREF INVALID KEY → DISPLAY 'ACCOUNT NOT FOUND: <key>' then status check still abends (status '23' → APPL-RESULT 12).

## Quirks (preserve bug-for-bug)

1. **Unreachable flush**: `PERFORM UNTIL END-OF-FILE='Y'` body always sees `END-OF-FILE='N'`, so the `ELSE → 1050-UPDATE-ACCOUNT` branch is dead code. Consequence: **the last account in TCATBALF never gets its REWRITE** — its interest TRANSACT rows exist but `ACCT-CURR-BAL` is not updated.
2. `1400-COMPUTE-FEES` is an empty stub ("To be implemented") — invoked but no-op.
3. One TRANSACT row **per category record**, not per account.
4. `DISPLAY TRAN-CAT-BAL-RECORD` dumps every input record to SYSOUT.

## Paragraph map

| Paragraph | Role |
|---|---|
| main (180-232) | open-all → loop → close-all → GOBACK |
| 0000/0100/0200/0300/0400 | opens (INPUT/I-O/OUTPUT) |
| 1000-TCATBALF-GET-NEXT | sequential READ, sets EOF on status 10 |
| 1050-UPDATE-ACCOUNT | apply total, REWRITE acct |
| 1100-GET-ACCT-DATA / 1110-GET-XREF-DATA | random reads, INVALID KEY → display then abend |
| 1200-GET-INTEREST-RATE | keyed read; '23' → 1200-A default retry |
| 1300-COMPUTE-INTEREST / 1300-B-WRITE-TX | monthly int + write TRANSACT |
| 1400-COMPUTE-FEES | stub |
| 9000-9400 | closes |
| Z-GET-DB2-FORMAT-TIMESTAMP | CURRENT-DATE → `YYYY-MM-DD-HH.MM.SS.NN0000` |
| 9910-DISPLAY-IO-STATUS / 9999-ABEND-PROGRAM | status decode + CEE3ABD abend |

```mermaid
flowchart TD
    A[Open 5 files] --> B{Loop until EOF}
    B -->|read next TCATBALF| C{EOF?}
    C -->|yes| D[Close all, GOBACK]
    C -->|no| E[DISPLAY record]
    E --> F{new acct-id?}
    F -->|yes, not first| G[1050: flush prev acct\nbal += total-int, REWRITE]
    F -->|yes| H[reset total, read ACCT + XREF]
    G --> H
    F -->|no| I[build DISCGRP key]
    H --> I
    I --> J[1200: read DISCGRP\n'23' → DEFAULT retry]
    J --> K{rate ≠ 0?}
    K -->|yes| L[1300: monthly int\ntotal += int\nwrite TRANSACT row]
    K -->|no| B
    L --> B
    note[/Last acct never flushed\n\(dead ELSE branch\)/] -.-> D
```
