       IDENTIFICATION DIVISION.
       PROGRAM-ID.    LOADCARD.
      * Loads line-sequential card data into the CARDFILE KSDS used by
      * CBACT02C: RECORD KEY FD-CARD-NUM (16) at offset 0, LRECL 150.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FLAT-FILE ASSIGN TO FLATIN
                  ORGANIZATION IS LINE SEQUENTIAL
                  FILE STATUS IS FLAT-STATUS.
           SELECT CARDFILE-FILE ASSIGN TO CARDFILE
                  ORGANIZATION IS INDEXED
                  ACCESS MODE IS DYNAMIC
                  RECORD KEY   IS FD-CARD-NUM
                  FILE STATUS  IS CARD-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  FLAT-FILE.
       01  FLAT-REC                     PIC X(150).
       FD  CARDFILE-FILE.
       01  FD-CARD-REC.
           05 FD-CARD-NUM               PIC X(16).
           05 FD-CARD-DATA              PIC X(134).
       WORKING-STORAGE SECTION.
       01  FLAT-STATUS                  PIC XX.
       01  CARD-STATUS                  PIC XX.
       01  WS-COUNT                     PIC 9(9) VALUE 0.
       01  END-OF-FILE                  PIC X VALUE 'N'.
       PROCEDURE DIVISION.
           OPEN INPUT FLAT-FILE
           OPEN OUTPUT CARDFILE-FILE
           PERFORM UNTIL END-OF-FILE = 'Y'
               READ FLAT-FILE
                   AT END MOVE 'Y' TO END-OF-FILE
                   NOT AT END
                       MOVE FLAT-REC TO FD-CARD-REC
                       WRITE FD-CARD-REC
                       ADD 1 TO WS-COUNT
               END-READ
           END-PERFORM
           CLOSE FLAT-FILE CARDFILE-FILE
           DISPLAY 'LOADED ' WS-COUNT ' CARD RECORDS'
           GOBACK.
