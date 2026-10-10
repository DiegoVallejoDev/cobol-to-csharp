       IDENTIFICATION DIVISION.
       PROGRAM-ID.    JCLDRV.
      * z/OS JCL PARM emulator: invokes the target batch module with
      * the standard EXEC PGM=...,PARM='...' linkage — a 2-byte binary
      * length followed by the PARM data — exactly as z/OS delivers it.
      * Usage: jcldrv <PARM-STRING>
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-ARG              PIC X(100).
       01  WS-LEN              PIC 9(04).
       01  CALL-PARMS.
           05  PARM-LENGTH     PIC S9(04) COMP.
           05  PARM-DATA       PIC X(100).
       PROCEDURE DIVISION.
           DISPLAY 1 UPON ARGUMENT-NUMBER
           ACCEPT WS-ARG FROM ARGUMENT-VALUE
           MOVE FUNCTION LENGTH(FUNCTION TRIM(WS-ARG)) TO WS-LEN
           MOVE WS-LEN TO PARM-LENGTH
           MOVE WS-ARG TO PARM-DATA
           CALL 'CBACT04C' USING CALL-PARMS
           GOBACK.
