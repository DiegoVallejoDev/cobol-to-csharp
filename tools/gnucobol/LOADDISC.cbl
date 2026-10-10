       IDENTIFICATION DIVISION.
       PROGRAM-ID.    LOADDISC.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FLAT-FILE ASSIGN TO FLATIN
                  ORGANIZATION IS LINE SEQUENTIAL
                  FILE STATUS IS FLAT-STATUS.
           SELECT DISCGRP-FILE ASSIGN TO DISCGRP
                  ORGANIZATION IS INDEXED
                  ACCESS MODE  IS DYNAMIC
                  RECORD KEY   IS FD-DISCGRP-KEY
                  FILE STATUS  IS DISCGRP-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  FLAT-FILE.
       01  FLAT-REC                         PIC X(50).
       FD  DISCGRP-FILE.
       01  FD-DISCGRP-REC.
           05 FD-DISCGRP-KEY.
              10 FD-DIS-ACCT-GROUP-ID       PIC X(10).
              10 FD-DIS-TRAN-TYPE-CD        PIC X(02).
              10 FD-DIS-TRAN-CAT-CD         PIC 9(04).
           05 FD-DISCGRP-DATA               PIC X(34).
       WORKING-STORAGE SECTION.
       01  FLAT-STATUS                      PIC XX.
       01  DISCGRP-STATUS                   PIC XX.
       01  WS-COUNT                         PIC 9(9) VALUE 0.
       01  END-OF-FILE                      PIC X VALUE 'N'.
       PROCEDURE DIVISION.
           OPEN INPUT FLAT-FILE
           IF FLAT-STATUS NOT = '00'
               DISPLAY 'FLAT OPEN ERR ' FLAT-STATUS
               STOP RUN RETURNING 12
           END-IF
           OPEN OUTPUT DISCGRP-FILE
           IF DISCGRP-STATUS NOT = '00'
               DISPLAY 'IDX OPEN ERR ' DISCGRP-STATUS
               STOP RUN RETURNING 12
           END-IF
           PERFORM UNTIL END-OF-FILE = 'Y'
               READ FLAT-FILE INTO FD-DISCGRP-REC
                   AT END MOVE 'Y' TO END-OF-FILE
                   NOT AT END
                       WRITE FD-DISCGRP-REC
                       IF DISCGRP-STATUS = '00'
                           ADD 1 TO WS-COUNT
                       ELSE
                           DISPLAY 'WRITE ERR ' DISCGRP-STATUS
                           STOP RUN RETURNING 12
                       END-IF
               END-READ
           END-PERFORM
           CLOSE FLAT-FILE DISCGRP-FILE
           DISPLAY 'LOADED ' WS-COUNT ' DISCGRP RECORDS'
           STOP RUN RETURNING 0.
