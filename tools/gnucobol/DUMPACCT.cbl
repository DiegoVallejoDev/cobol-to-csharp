       IDENTIFICATION DIVISION.
       PROGRAM-ID.    DUMPACCT.
      * Dumps indexed account file to flat file in key order,
      * for byte-exact post-run comparison.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FLAT-FILE ASSIGN TO FLATOUT
                  ORGANIZATION IS RECORD SEQUENTIAL
                  FILE STATUS IS FLAT-STATUS.
           SELECT ACCTFILE-FILE ASSIGN TO ACCTFILE
                  ORGANIZATION IS INDEXED
                  ACCESS MODE  IS SEQUENTIAL
                  RECORD KEY   IS FD-ACCT-ID
                  FILE STATUS  IS ACCTFILE-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  FLAT-FILE.
       01  FLAT-REC                         PIC X(300).
       FD  ACCTFILE-FILE.
       01  FD-ACCTFILE-REC.
           05 FD-ACCT-ID                    PIC 9(11).
           05 FD-ACCT-DATA                  PIC X(289).
       WORKING-STORAGE SECTION.
       01  FLAT-STATUS                      PIC XX.
       01  ACCTFILE-STATUS                  PIC XX.
       01  WS-COUNT                         PIC 9(9) VALUE 0.
       01  END-OF-FILE                      PIC X VALUE 'N'.
       PROCEDURE DIVISION.
           OPEN INPUT ACCTFILE-FILE
           IF ACCTFILE-STATUS NOT = '00'
               DISPLAY 'IDX OPEN ERR ' ACCTFILE-STATUS
               STOP RUN RETURNING 12
           END-IF
           OPEN OUTPUT FLAT-FILE
           IF FLAT-STATUS NOT = '00'
               DISPLAY 'FLAT OPEN ERR ' FLAT-STATUS
               STOP RUN RETURNING 12
           END-IF
           PERFORM UNTIL END-OF-FILE = 'Y'
               READ ACCTFILE-FILE
                   AT END MOVE 'Y' TO END-OF-FILE
                   NOT AT END
                       MOVE FD-ACCTFILE-REC TO FLAT-REC
                       WRITE FLAT-REC
                       IF FLAT-STATUS = '00'
                           ADD 1 TO WS-COUNT
                       ELSE
                           DISPLAY 'WRITE ERR ' FLAT-STATUS
                           STOP RUN RETURNING 12
                       END-IF
               END-READ
           END-PERFORM
           CLOSE FLAT-FILE ACCTFILE-FILE
           DISPLAY 'DUMPED ' WS-COUNT ' ACCT RECORDS'
           STOP RUN RETURNING 0.
