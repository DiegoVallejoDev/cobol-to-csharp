       IDENTIFICATION DIVISION.
       PROGRAM-ID.    CEE3ABD.
      * Minimal stand-in for the Language Environment abend routine.
      * Real CEE3ABD terminates the enclave with the supplied code;
      * here we display it and exit nonzero so harnesses see the abend.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-ABCODE   PIC S9(9) BINARY.
       01  LK-TIMING   PIC S9(9) BINARY.
       PROCEDURE DIVISION USING LK-ABCODE, LK-TIMING.
           DISPLAY 'CEE3ABD: ABEND CODE ' LK-ABCODE.
           STOP RUN RETURNING 16.
