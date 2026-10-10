       IDENTIFICATION DIVISION.
       PROGRAM-ID.    LOADTCAT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FLAT-FILE ASSIGN TO FLATIN
                  ORGANIZATION IS LINE SEQUENTIAL
                  FILE STATUS IS FLAT-STATUS.
           SELECT TCATBAL-FILE ASSIGN TO TCATBALF
                  ORGANIZATION IS INDEXED
                  ACCESS MODE  IS SEQUENTIAL
                  RECORD KEY   IS FD-TRAN-CAT-KEY
                  FILE STATUS  IS TCATBALF-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  FLAT-FILE.
       01  FLAT-REC                         PIC X(50).
       FD  TCATBAL-FILE.
       01  FD-TRAN-CAT-BAL-RECORD.
           05 FD-TRAN-CAT-KEY.
              10 FD-TRANCAT-ACCT-ID         PIC 9(11).
              10 FD-TRANCAT-TYPE-CD         PIC X(02).
              10 FD-TRANCAT-CD              PIC 9(04).
           05 FD-FD-TRAN-CAT-DATA           PIC X(33).
       WORKING-STORAGE SECTION.
       01  FLAT-STATUS                      PIC XX.
       01  TCATBALF-STATUS                  PIC XX.
       01  WS-COUNT                         PIC 9(9) VALUE 0.
       01  END-OF-FILE                      PIC X VALUE 'N'.
       PROCEDURE DIVISION.
           OPEN INPUT FLAT-FILE
           IF FLAT-STATUS NOT = '00'
               DISPLAY 'FLAT OPEN ERR ' FLAT-STATUS
               STOP RUN RETURNING 12
           END-IF
           OPEN OUTPUT TCATBAL-FILE
           IF TCATBALF-STATUS NOT = '00'
               DISPLAY 'IDX OPEN ERR ' TCATBALF-STATUS
               STOP RUN RETURNING 12
           END-IF
           PERFORM UNTIL END-OF-FILE = 'Y'
               READ FLAT-FILE INTO FD-TRAN-CAT-BAL-RECORD
                   AT END MOVE 'Y' TO END-OF-FILE
                   NOT AT END
                       WRITE FD-TRAN-CAT-BAL-RECORD
                       IF TCATBALF-STATUS = '00'
                           ADD 1 TO WS-COUNT
                       ELSE
                           DISPLAY 'WRITE ERR ' TCATBALF-STATUS
                           STOP RUN RETURNING 12
                       END-IF
               END-READ
           END-PERFORM
           CLOSE FLAT-FILE TCATBAL-FILE
           DISPLAY 'LOADED ' WS-COUNT ' TCATBAL RECORDS'
           STOP RUN RETURNING 0.
