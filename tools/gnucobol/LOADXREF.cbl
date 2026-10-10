       IDENTIFICATION DIVISION.
       PROGRAM-ID.    LOADXREF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FLAT-FILE ASSIGN TO FLATIN
                  ORGANIZATION IS LINE SEQUENTIAL
                  FILE STATUS IS FLAT-STATUS.
           SELECT XREF-FILE ASSIGN TO XREFFILE
                  ORGANIZATION IS INDEXED
                  ACCESS MODE  IS DYNAMIC
                  RECORD KEY   IS FD-XREF-CARD-NUM
                  ALTERNATE RECORD KEY IS FD-XREF-ACCT-ID
                  FILE STATUS  IS XREFFILE-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  FLAT-FILE.
       01  FLAT-REC                         PIC X(50).
       FD  XREF-FILE.
       01  FD-XREFFILE-REC.
           05 FD-XREF-CARD-NUM              PIC X(16).
           05 FD-XREF-CUST-NUM              PIC 9(09).
           05 FD-XREF-ACCT-ID               PIC 9(11).
           05 FD-XREF-FILLER                PIC X(14).
       WORKING-STORAGE SECTION.
       01  FLAT-STATUS                      PIC XX.
       01  XREFFILE-STATUS                  PIC XX.
       01  WS-COUNT                         PIC 9(9) VALUE 0.
       01  END-OF-FILE                      PIC X VALUE 'N'.
       PROCEDURE DIVISION.
           OPEN INPUT FLAT-FILE
           IF FLAT-STATUS NOT = '00'
               DISPLAY 'FLAT OPEN ERR ' FLAT-STATUS
               STOP RUN RETURNING 12
           END-IF
           OPEN OUTPUT XREF-FILE
           IF XREFFILE-STATUS NOT = '00'
               DISPLAY 'IDX OPEN ERR ' XREFFILE-STATUS
               STOP RUN RETURNING 12
           END-IF
           PERFORM UNTIL END-OF-FILE = 'Y'
               READ FLAT-FILE INTO FD-XREFFILE-REC
                   AT END MOVE 'Y' TO END-OF-FILE
                   NOT AT END
                       WRITE FD-XREFFILE-REC
                       IF XREFFILE-STATUS = '00'
                           ADD 1 TO WS-COUNT
                       ELSE
                           DISPLAY 'WRITE ERR ' XREFFILE-STATUS
                           STOP RUN RETURNING 12
                       END-IF
               END-READ
           END-PERFORM
           CLOSE FLAT-FILE XREF-FILE
           DISPLAY 'LOADED ' WS-COUNT ' XREF RECORDS'
           STOP RUN RETURNING 0.
