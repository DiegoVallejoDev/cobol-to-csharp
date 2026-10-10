       IDENTIFICATION DIVISION.
       PROGRAM-ID.    COBDATFT.
      * COBOL stand-in for the HLASM COBDATFT date-format routine so the
      * original programs can run under GnuCOBOL for golden-master runs.
      * Semantics transcribed from app/asm/COBDATFT.asm:
      *   type '1' (YYYYMMDD)  + outtype '1' -> 'YYYY-MM-DD' (insert dashes)
      *   type '2' (YYYY-MM-DD)+ outtype '2' -> 'YYYYMMDD'   (strip dashes)
      *   any other combination -> 'INVALID INPUT' in ERROR-MSG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY CODATECN.
       LINKAGE SECTION.
       01  LK-REC                      PIC X(80).
       PROCEDURE DIVISION USING LK-REC.
           MOVE LK-REC TO CODATECN-REC
           IF CODATECN-TYPE = '1' AND CODATECN-OUTTYPE = '1'
               STRING CODATECN-INP-DATE(1:4) '-'
                      CODATECN-INP-DATE(5:2) '-'
                      CODATECN-INP-DATE(7:2)
                   INTO CODATECN-0UT-DATE
           ELSE IF CODATECN-TYPE = '2' AND CODATECN-OUTTYPE = '2'
               STRING CODATECN-INP-DATE(1:4)
                      CODATECN-INP-DATE(6:2)
                      CODATECN-INP-DATE(9:2)
                   INTO CODATECN-0UT-DATE
           ELSE
               MOVE 'INVALID INPUT' TO CODATECN-ERROR-MSG
           END-IF
           MOVE CODATECN-REC TO LK-REC
           GOBACK.
