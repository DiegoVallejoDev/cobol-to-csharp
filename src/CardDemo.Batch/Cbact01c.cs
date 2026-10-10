using System;
using System.IO;
using Legacy.Runtime;

namespace CardDemo.Batch;

/// <summary>
/// Faithful port of CBACT01C (READACCT job): reads ACCTFILE sequentially and
/// rewrites each account into three outputs — OUTFILE (fixed 107B, one COMP-3
/// field), ARRYFILE (fixed 110B, OCCURS array with COMP-3), VBRCFILE
/// (variable-length RDW records) — calling COBDATFT for the reissue date.
/// </summary>
public sealed class Cbact01c
{
    private readonly IndexedFile _acct;
    private readonly SequentialFileWriter _out;
    private readonly SequentialFileWriter _arry;
    private readonly VariableLengthFileWriter _vbrc;
    private string _acctStatus = FileStatus.Ok;
    private string _outStatus = FileStatus.Ok;
    private string _arryStatus = FileStatus.Ok;
    private string _vbrcStatus = FileStatus.Ok;
    private string _ioStatus = "  ";
    private int _applResult;
    private char _endOfFile = 'N';

    private byte[] _acctRec = new byte[300];
    private readonly byte[] _outRec = CobolFields.Latin1.GetBytes(new string(' ', 107));
    private readonly byte[] _arrRec = new byte[110];
    private readonly byte[] _vbrcRec1 = new byte[12];
    private readonly byte[] _vbrcRec2 = new byte[39];
    private readonly byte[] _codate = CobolFields.Latin1.GetBytes(new string(' ', 80));
    private readonly byte[] _wsReissue = new byte[10];

    private bool ApplAok => _applResult == 0;
    private bool ApplEof => _applResult == 16;

    public Cbact01c(string dataDir)
    {
        _acct = new IndexedFile(Path.Combine(dataDir, "acctdata"), 300, 0, 11);
        _out = new SequentialFileWriter(Path.Combine(dataDir, "OUTFILE"), 107);
        _arry = new SequentialFileWriter(Path.Combine(dataDir, "ARRYFILE"), 110);
        _vbrc = new VariableLengthFileWriter(Path.Combine(dataDir, "VBRCFILE"));
    }

    public int Run()
    {
        Sysout.Display("START OF EXECUTION OF PROGRAM CBACT01C");
        P0000_AcctfileOpen();
        P2000_OutfileOpen();
        P3000_ArrfileOpen();
        P4000_VbrfileOpen();

        while (_endOfFile != 'Y')
        {
            if (_endOfFile == 'N')
            {
                P1000_AcctfileGetNext();
                if (_endOfFile == 'N')
                    Sysout.Display(_acctRec);
            }
        }

        P9000_AcctfileClose();
        _out.Close(); _arry.Close(); _vbrc.Close();
        Sysout.Display("END OF EXECUTION OF PROGRAM CBACT01C");
        return 0;
    }

    // ---- 1000 read driver -------------------------------------------------
    private void P1000_AcctfileGetNext()
    {
        _acctStatus = _acct.ReadNext();
        if (_acctStatus == FileStatus.Ok)
        {
            _applResult = 0;
            _acctRec = _acct.Current!;
            InitializeArrRec();
            P1100_DisplayAcctRecord();
            P1300_PopulAcctRecord();
            P1350_WriteAcctRecord();
            P1400_PopulArrayRecord();
            P1450_WriteArryRecord();
            InitializeVbrcRec1();
            P1500_PopulVbrcRecord();
            P1550_WriteVb1Record();
            P1575_WriteVb2Record();
            return;
        }
        _applResult = _acctStatus == FileStatus.EndOfFile ? 16 : 12;
        if (ApplEof) { _endOfFile = 'Y'; return; }
        Sysout.Display("ERROR READING ACCOUNT FILE");
        _ioStatus = _acctStatus;
        P9910_DisplayIoStatus();
        P9999_AbendProgram();
    }

    // ---- 1100 display -----------------------------------------------------
    private void P1100_DisplayAcctRecord()
    {
        Sysout.Display("ACCT-ID                 :" + CobolFields.GetString(_acctRec, 0, 11));
        Sysout.Display("ACCT-ACTIVE-STATUS      :" + CobolFields.GetString(_acctRec, 11, 1));
        Sysout.Display("ACCT-CURR-BAL           :" + ZonedDecimal.ToDisplayString(_acctRec.AsSpan(12, 12), 2));
        Sysout.Display("ACCT-CREDIT-LIMIT       :" + ZonedDecimal.ToDisplayString(_acctRec.AsSpan(24, 12), 2));
        Sysout.Display("ACCT-CASH-CREDIT-LIMIT  :" + ZonedDecimal.ToDisplayString(_acctRec.AsSpan(36, 12), 2));
        Sysout.Display("ACCT-OPEN-DATE          :" + CobolFields.GetString(_acctRec, 48, 10));
        Sysout.Display("ACCT-EXPIRAION-DATE     :" + CobolFields.GetString(_acctRec, 58, 10));
        Sysout.Display("ACCT-REISSUE-DATE       :" + CobolFields.GetString(_acctRec, 68, 10));
        Sysout.Display("ACCT-CURR-CYC-CREDIT    :" + ZonedDecimal.ToDisplayString(_acctRec.AsSpan(78, 12), 2));
        Sysout.Display("ACCT-CURR-CYC-DEBIT     :" + ZonedDecimal.ToDisplayString(_acctRec.AsSpan(90, 12), 2));
        Sysout.Display("ACCT-GROUP-ID           :" + CobolFields.GetString(_acctRec, 112, 10));
        Sysout.Display("-------------------------------------------------");
    }

    // ---- 1300 populate output record ----------------------------------------
    private void P1300_PopulAcctRecord()
    {
        Array.Copy(_acctRec, 0, _outRec, 0, 11);         // OUT-ACCT-ID
        _outRec[11] = _acctRec[11];                       // OUT-ACCT-ACTIVE-STATUS
        Array.Copy(_acctRec, 12, _outRec, 12, 12);        // OUT-ACCT-CURR-BAL
        Array.Copy(_acctRec, 24, _outRec, 24, 12);        // OUT-ACCT-CREDIT-LIMIT
        Array.Copy(_acctRec, 36, _outRec, 36, 12);        // OUT-ACCT-CASH-CREDIT-LIMIT
        Array.Copy(_acctRec, 48, _outRec, 48, 10);        // OUT-ACCT-OPEN-DATE
        Array.Copy(_acctRec, 58, _outRec, 58, 10);        // OUT-ACCT-EXPIRAION-DATE
        // MOVE ACCT-REISSUE-DATE TO CODATECN-INP-DATE + WS-REISSUE-DATE
        CobolFields.SetString(_codate, 1, 20, CobolFields.GetString(_acctRec, 68, 10));
        Array.Copy(_acctRec, 68, _wsReissue, 0, 10);
        _codate[0] = (byte)'2';                           // CODATECN-TYPE
        _codate[21] = (byte)'2';                          // CODATECN-OUTTYPE
        Cobdatft(_codate);                                // CALL 'COBDATFT'
        Array.Copy(_codate, 22, _outRec, 68, 10);         // OUT-ACCT-REISSUE-DATE
        Array.Copy(_acctRec, 78, _outRec, 78, 12);        // OUT-ACCT-CURR-CYC-CREDIT
        if (ZonedDecimal.Parse(_acctRec.AsSpan(90, 12), 2) == 0m)
            PackedDecimal.Write(_outRec.AsSpan(90, 7), 2525.00m, 2);
        // else: bytes retain prior contents — faithful to the missing ELSE
        Array.Copy(_acctRec, 112, _outRec, 97, 10);       // OUT-ACCT-GROUP-ID
    }

    // ---- 1350 / 1400 / 1450 -------------------------------------------------
    private void P1350_WriteAcctRecord()
    {
        _outStatus = _out.Write(_outRec);
        if (_outStatus is not (FileStatus.Ok or FileStatus.EndOfFile))
        {
            Sysout.Display("ACCOUNT FILE WRITE STATUS IS:" + _outStatus);
            _ioStatus = _outStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P1400_PopulArrayRecord()
    {
        Array.Copy(_acctRec, 0, _arrRec, 0, 11);                // ARR-ACCT-ID
        Array.Copy(_acctRec, 12, _arrRec, 11, 12);              // bal(1)
        PackedDecimal.Write(_arrRec.AsSpan(23, 7), 1005.00m, 2);// deb(1)
        Array.Copy(_acctRec, 12, _arrRec, 30, 12);              // bal(2)
        PackedDecimal.Write(_arrRec.AsSpan(42, 7), 1525.00m, 2);// deb(2)
        ZonedDecimal.Write(_arrRec.AsSpan(49, 12), -1025.00m, 2);// bal(3)
        PackedDecimal.Write(_arrRec.AsSpan(61, 7), -2500.00m, 2);// deb(3)
    }

    private void P1450_WriteArryRecord()
    {
        _arryStatus = _arry.Write(_arrRec);
        if (_arryStatus is not (FileStatus.Ok or FileStatus.EndOfFile))
        {
            Sysout.Display("ACCOUNT FILE WRITE STATUS IS:" + _arryStatus);
            _ioStatus = _arryStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    // ---- 1500 / 1550 / 1575 --------------------------------------------------
    private void P1500_PopulVbrcRecord()
    {
        Array.Copy(_acctRec, 0, _vbrcRec1, 0, 11);      // VB1-ACCT-ID
        Array.Copy(_acctRec, 0, _vbrcRec2, 0, 11);      // VB2-ACCT-ID
        _vbrcRec1[11] = _acctRec[11];                    // VB1-ACCT-ACTIVE-STATUS
        Array.Copy(_acctRec, 12, _vbrcRec2, 11, 12);     // VB2-ACCT-CURR-BAL
        Array.Copy(_acctRec, 24, _vbrcRec2, 23, 12);     // VB2-ACCT-CREDIT-LIMIT
        Array.Copy(_wsReissue, 0, _vbrcRec2, 35, 4);     // VB2-ACCT-REISSUE-YYYY
        Sysout.Display("VBRC-REC1:" + CobolFields.Latin1.GetString(_vbrcRec1));
        Sysout.Display("VBRC-REC2:" + CobolFields.Latin1.GetString(_vbrcRec2));
    }

    private void P1550_WriteVb1Record()
    {
        _vbrcStatus = _vbrc.Write(_vbrcRec1, 12);
        if (_vbrcStatus is not (FileStatus.Ok or FileStatus.EndOfFile))
        {
            Sysout.Display("ACCOUNT FILE WRITE STATUS IS:" + _vbrcStatus);
            _ioStatus = _vbrcStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P1575_WriteVb2Record()
    {
        _vbrcStatus = _vbrc.Write(_vbrcRec2, 39);
        if (_vbrcStatus is not (FileStatus.Ok or FileStatus.EndOfFile))
        {
            Sysout.Display("ACCOUNT FILE WRITE STATUS IS:" + _vbrcStatus);
            _ioStatus = _vbrcStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    // ---- INITIALIZE semantics (numeric->'0', alpha->' ', COMP-3->packed 0) ----
    private void InitializeArrRec()
    {
        CobolFields.Fill(_arrRec, 0, 11, (byte)'0');
        for (int r = 0; r < 5; r++)
        {
            CobolFields.Fill(_arrRec, 11 + r * 19, 12, (byte)'0');
            PackedDecimal.Write(_arrRec.AsSpan(11 + r * 19 + 12, 7), 0m, 2);
        }
        CobolFields.Fill(_arrRec, 106, 4, (byte)' ');
    }

    private void InitializeVbrcRec1()
    {
        CobolFields.Fill(_vbrcRec1, 0, 11, (byte)'0');
        _vbrcRec1[11] = (byte)' ';
    }

    /// <summary>COBDATFT port — see tools/gnucobol/COBDATFT.cbl.</summary>
    private static void Cobdatft(byte[] rec)
    {
        byte type = rec[0], outtype = rec[21];
        if (type == (byte)'1' && outtype == (byte)'1')
        {
            // YYYYMMDD -> YYYY-MM-DD
            rec[22] = rec[1]; rec[23] = rec[2]; rec[24] = rec[3]; rec[25] = rec[4];
            rec[26] = (byte)'-';
            rec[27] = rec[5]; rec[28] = rec[6];
            rec[29] = (byte)'-';
            rec[30] = rec[7]; rec[31] = rec[8];
        }
        else if (type == (byte)'2' && outtype == (byte)'2')
        {
            // YYYY-MM-DD -> YYYYMMDD
            rec[22] = rec[1]; rec[23] = rec[2]; rec[24] = rec[3]; rec[25] = rec[4];
            rec[26] = rec[6]; rec[27] = rec[7];
            rec[28] = rec[9]; rec[29] = rec[10];
        }
        else
        {
            CobolFields.SetString(rec, 42, 38, "INVALID INPUT");
        }
    }

    // ---- opens / closes / errors --------------------------------------------
    private void P0000_AcctfileOpen()
    {
        _applResult = 8;
        _acctStatus = _acct.Open();
        _applResult = _acctStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR OPENING ACCTFILE");
            _ioStatus = _acctStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P2000_OutfileOpen() { _outStatus = FileStatus.Ok; _applResult = 0; }
    private void P3000_ArrfileOpen() { _arryStatus = FileStatus.Ok; _applResult = 0; }
    private void P4000_VbrfileOpen() { _vbrcStatus = FileStatus.Ok; _applResult = 0; }

    private void P9000_AcctfileClose()
    {
        _applResult = 8;
        _acctStatus = _acct.Close();
        _applResult = _acctStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR CLOSING ACCTFILE");
            _ioStatus = _acctStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P9910_DisplayIoStatus() => Abend.DisplayIoStatus(_ioStatus);
    private void P9999_AbendProgram() => Abend.Cee3abd();
}
