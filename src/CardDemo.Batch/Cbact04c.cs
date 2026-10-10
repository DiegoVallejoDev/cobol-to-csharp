using System;
using System.IO;
using Legacy.Runtime;

namespace CardDemo.Batch;

/// <summary>
/// Faithful port of CBACT04C (CardDemo interest calculator, INTCALC job).
/// Paragraph names are preserved as method names; COBOL control flow and
/// semantics are reproduced exactly — including quirks documented in
/// docs/analysis/CBACT04C.md (dead ELSE branch, COMPUTE-FEES stub).
/// </summary>
public sealed class Cbact04c
{
    // ---- File-control state (record layouts per migration-map/CBACT04C.json)
    private readonly IndexedFile _tcatbal;
    private readonly IndexedFile _xref;
    private readonly IndexedFile _acct;
    private readonly IndexedFile _disc;
    private readonly SequentialFileWriter _transact;
    private string _tcatbalStatus = FileStatus.Ok;
    private string _xrefStatus = FileStatus.Ok;
    private string _acctStatus = FileStatus.Ok;
    private string _discStatus = FileStatus.Ok;
    private string _tranStatus = FileStatus.Ok;

    // ---- FD key fields
    private readonly byte[] _fdAcctId = new byte[11];
    private readonly byte[] _fdXrefAcctId = new byte[11];
    private readonly byte[] _fdDiscgrpKey = new byte[16];   // grp 10 + type 2 + cat 4

    // ---- Working storage records (bytes, as COBOL)
    private byte[] _tranCatRec = new byte[50];
    private byte[] _acctRec = new byte[300];
    private byte[] _xrefRec = new byte[50];
    private byte[] _discRec = new byte[50];
    private readonly byte[] _tranRec = CobolFields.Latin1.GetBytes(new string(' ', 350));

    // ---- WS vars
    private string _ioStatus = "  ";
    private int _applResult;
    private char _endOfFile = 'N';
    private readonly string _parmDate;                      // EXTERNAL-PARMS PARM-DATE X(10)
    private string _wsLastAcctNum = "           ";
    private decimal _wsMonthlyInt;
    private decimal _wsTotalInt;
    private char _wsFirstTime = 'Y';
    private int _wsRecordCount;
    private int _wsTranIdSuffix;

    private bool ApplAok => _applResult == 0;
    private bool ApplEof => _applResult == 16;

    public Cbact04c(string dataDir, string parmDate)
    {
        _parmDate = parmDate;
        _tcatbal = new IndexedFile(Path.Combine(dataDir, "tcatbal"), 50, 0, 17);
        _xref = new IndexedFile(Path.Combine(dataDir, "cardxref"), 50, 0, 16, altKeyOffset: 25, altKeyLength: 11);
        _acct = new IndexedFile(Path.Combine(dataDir, "acctdata"), 300, 0, 11, persist: true);
        _disc = new IndexedFile(Path.Combine(dataDir, "discgrp"), 50, 0, 16);
        _transact = new SequentialFileWriter(Path.Combine(dataDir, "TRANSACT"), 350);
    }

    public int Run()
    {
        // ------------------------------------------------ main (lines 180-232)
        Sysout.Display("START OF EXECUTION OF PROGRAM CBACT04C");
        P0000_TcatbalFOpen();
        P0100_XrefFileOpen();
        P0200_DiscgrpOpen();
        P0300_AcctFileOpen();
        P0400_TranFileOpen();

        while (_endOfFile != 'Y')
        {
            if (_endOfFile == 'N')
            {
                P1000_TcatbalFGetNext();
                if (_endOfFile == 'N')
                {
                    _wsRecordCount++;
                    Sysout.Display(_tranCatRec);
                    string tranCatAcctId = CobolFields.GetString(_tranCatRec, 0, 11);
                    if (tranCatAcctId != _wsLastAcctNum)
                    {
                        if (_wsFirstTime != 'Y')
                            P1050_UpdateAccount();
                        else
                            _wsFirstTime = 'N';
                        _wsTotalInt = 0;
                        _wsLastAcctNum = tranCatAcctId;
                        CobolFields.Latin1.GetBytes(tranCatAcctId).CopyTo(_fdAcctId, 0);
                        P1100_GetAcctData();
                        CobolFields.Latin1.GetBytes(tranCatAcctId).CopyTo(_fdXrefAcctId, 0);
                        P1110_GetXrefData();
                    }
                    // FD-DISCGRP-KEY = grp(10) + type(2) + cat(4)
                    Array.Copy(_acctRec, 112, _fdDiscgrpKey, 0, 10);
                    Array.Copy(_tranCatRec, 11, _fdDiscgrpKey, 10, 2);
                    Array.Copy(_tranCatRec, 13, _fdDiscgrpKey, 12, 4);
                    P1200_GetInterestRate();
                    if (DisIntRate != 0)
                    {
                        P1300_ComputeInterest();
                        P1400_ComputeFees();
                    }
                }
            }
            else
            {
                P1050_UpdateAccount();   // dead code — unreachable, kept bug-for-bug
            }
        }

        P9000_TcatbalFClose();
        P9100_XrefFileClose();
        P9200_DiscgrpClose();
        P9300_AcctFileClose();
        P9400_TranFileClose();

        Sysout.Display("END OF EXECUTION OF PROGRAM CBACT04C");
        return 0;
    }

    private decimal DisIntRate => ZonedDecimal.Parse(_discRec.AsSpan(16, 6), 2);

    // ---- 0000-0400 opens ------------------------------------------------
    private void P0000_TcatbalFOpen()
    {
        _applResult = 8;
        _tcatbalStatus = _tcatbal.Open();
        _applResult = _tcatbalStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR OPENING TRANSACTION CATEGORY BALANCE");
            _ioStatus = _tcatbalStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P0100_XrefFileOpen()
    {
        _applResult = 8;
        _xrefStatus = _xref.Open();
        _applResult = _xrefStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR OPENING CROSS REF FILE " + _xrefStatus);
            _ioStatus = _xrefStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P0200_DiscgrpOpen()
    {
        _applResult = 8;
        _discStatus = _disc.Open();
        _applResult = _discStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR OPENING DALY REJECTS FILE");
            _ioStatus = _discStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P0300_AcctFileOpen()
    {
        _applResult = 8;
        _acctStatus = _acct.Open();
        _applResult = _acctStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR OPENING ACCOUNT MASTER FILE");
            _ioStatus = _acctStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P0400_TranFileOpen()
    {
        _applResult = 8;
        _tranStatus = FileStatus.Ok;   // SequentialFileWriter ctor creates file
        _applResult = 0;
        if (!ApplAok)
        {
            Sysout.Display("ERROR OPENING TRANSACTION FILE");
            _ioStatus = _tranStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    // ---- 1000 read driver ------------------------------------------------
    private void P1000_TcatbalFGetNext()
    {
        _tcatbalStatus = _tcatbal.ReadNext();
        if (_tcatbalStatus == FileStatus.Ok) _applResult = 0;
        else if (_tcatbalStatus == FileStatus.EndOfFile) _applResult = 16;
        else _applResult = 12;

        if (ApplAok)
        {
            _tranCatRec = _tcatbal.Current!;
            return;
        }
        if (ApplEof)
        {
            _endOfFile = 'Y';
            return;
        }
        Sysout.Display("ERROR READING TRANSACTION CATEGORY FILE");
        _ioStatus = _tcatbalStatus;
        P9910_DisplayIoStatus();
        P9999_AbendProgram();
    }

    // ---- 1050 update account ----------------------------------------------
    private void P1050_UpdateAccount()
    {
        // ADD WS-TOTAL-INT TO ACCT-CURR-BAL; cyc credit/debit := 0; REWRITE
        decimal bal = ZonedDecimal.Parse(_acctRec.AsSpan(12, 12), 2) + _wsTotalInt;
        ZonedDecimal.Write(_acctRec.AsSpan(12, 12), bal, 2);
        ZonedDecimal.Write(_acctRec.AsSpan(78, 12), 0m, 2);
        ZonedDecimal.Write(_acctRec.AsSpan(90, 12), 0m, 2);
        _acctStatus = _acct.Rewrite(_acctRec);
        _applResult = _acctStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR RE-WRITING ACCOUNT FILE");
            _ioStatus = _acctStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    // ---- 1100/1110 keyed reads ---------------------------------------------
    private void P1100_GetAcctData()
    {
        _acctStatus = _acct.ReadKey(_fdAcctId);
        if (_acctStatus != FileStatus.Ok)
            Sysout.Display("ACCOUNT NOT FOUND: " + CobolFields.Latin1.GetString(_fdAcctId));
        else
            _acctRec = _acct.Current!;
        _applResult = _acctStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR READING ACCOUNT FILE");
            _ioStatus = _acctStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P1110_GetXrefData()
    {
        _xrefStatus = _xref.ReadKey(_fdXrefAcctId, alt: true);
        if (_xrefStatus != FileStatus.Ok)
            Sysout.Display("ACCOUNT NOT FOUND: " + CobolFields.Latin1.GetString(_fdXrefAcctId));
        else
            _xrefRec = _xref.Current!;
        _applResult = _xrefStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR READING XREF FILE");
            _ioStatus = _xrefStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    // ---- 1200 disclosure-group rate lookup ----------------------------------
    private void P1200_GetInterestRate()
    {
        _discStatus = _disc.ReadKey(_fdDiscgrpKey);
        if (_discStatus != FileStatus.Ok)
        {
            Sysout.Display("DISCLOSURE GROUP RECORD MISSING");
            Sysout.Display("TRY WITH DEFAULT GROUP CODE");
        }
        else
        {
            _discRec = _disc.Current!;
        }
        _applResult = _discStatus is FileStatus.Ok or FileStatus.RecordNotFound ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR READING DISCLOSURE GROUP FILE");
            _ioStatus = _discStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
        if (_discStatus == FileStatus.RecordNotFound)
        {
            CobolFields.SetString(_fdDiscgrpKey, 0, 10, "DEFAULT");
            P1200A_GetDefaultIntRate();
        }
    }

    private void P1200A_GetDefaultIntRate()
    {
        _discStatus = _disc.ReadKey(_fdDiscgrpKey);
        if (_discStatus == FileStatus.Ok) _discRec = _disc.Current!;
        _applResult = _discStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR READING DEFAULT DISCLOSURE GROUP");
            _ioStatus = _discStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    // ---- 1300 interest + transaction write -----------------------------------
    private void P1300_ComputeInterest()
    {
        // COMPUTE WS-MONTHLY-INT = (TRAN-CAT-BAL * DIS-INT-RATE) / 1200
        // COBOL COMPUTE truncates toward zero at the target's 2 decimals.
        decimal bal = ZonedDecimal.Parse(_tranCatRec.AsSpan(17, 11), 2);
        decimal product = bal * DisIntRate / 1200m;
        _wsMonthlyInt = decimal.Truncate(product * 100m) / 100m;
        _wsTotalInt += _wsMonthlyInt;
        P1300B_WriteTx();
    }

    private void P1300B_WriteTx()
    {
        _wsTranIdSuffix++;
        // TRAN-ID = PARM-DATE + 6-digit suffix (STRING DELIMITED BY SIZE)
        string tranId = (_parmDate + _wsTranIdSuffix.ToString("D6")).PadRight(16)[..16];
        CobolFields.SetString(_tranRec, 0, 16, tranId);
        CobolFields.SetString(_tranRec, 16, 2, "01");
        CobolFields.SetString(_tranRec, 18, 4, "0005");
        CobolFields.SetString(_tranRec, 22, 10, "System");
        // STRING 'Int. for a/c ' ACCT-ID INTO TRAN-DESC — left-aligned overwrite
        string desc = "Int. for a/c " + CobolFields.GetString(_acctRec, 0, 11);
        var descBytes = CobolFields.Latin1.GetBytes(desc);
        Array.Copy(descBytes, 0, _tranRec, 32, Math.Min(descBytes.Length, 100));
        ZonedDecimal.Write(_tranRec.AsSpan(132, 11), _wsMonthlyInt, 2);
        CobolFields.SetString(_tranRec, 143, 9, "000000000");
        CobolFields.Fill(_tranRec, 152, 50, (byte)' ');
        CobolFields.Fill(_tranRec, 202, 50, (byte)' ');
        CobolFields.Fill(_tranRec, 252, 10, (byte)' ');
        Array.Copy(_xrefRec, 0, _tranRec, 262, 16);        // TRAN-CARD-NUM
        string ts = Db2Timestamp();
        CobolFields.SetString(_tranRec, 278, 26, ts);      // TRAN-ORIG-TS
        CobolFields.SetString(_tranRec, 304, 26, ts);      // TRAN-PROC-TS

        _tranStatus = _transact.Write(_tranRec);
        _applResult = _tranStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR WRITING TRANSACTION RECORD");
            _ioStatus = _tranStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    /// <summary>1400-COMPUTE-FEES — stub in source ("To be implemented").</summary>
    private void P1400_ComputeFees() { }

    // ---- 9000-9400 closes -----------------------------------------------------
    private void P9000_TcatbalFClose()
    {
        _applResult = 8;
        _tcatbalStatus = _tcatbal.Close();
        _applResult = _tcatbalStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR CLOSING TRANSACTION BALANCE FILE");
            _ioStatus = _tcatbalStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P9100_XrefFileClose()
    {
        _applResult = 8;
        _xrefStatus = _xref.Close();
        _applResult = _xrefStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR CLOSING CROSS REF FILE");
            _ioStatus = _xrefStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P9200_DiscgrpClose()
    {
        _applResult = 8;
        _discStatus = _disc.Close();
        _applResult = _discStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR CLOSING DISCLOSURE GROUP FILE");
            _ioStatus = _discStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P9300_AcctFileClose()
    {
        _applResult = 8;
        _acctStatus = _acct.Close();
        _applResult = _acctStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR CLOSING ACCOUNT FILE");
            _ioStatus = _acctStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P9400_TranFileClose()
    {
        _applResult = 8;
        _tranStatus = _transact.Close();
        _applResult = _tranStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR CLOSING TRANSACTION FILE");
            _ioStatus = _tranStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    // ---- Z-GET-DB2-FORMAT-TIMESTAMP --------------------------------------------
    private static string Db2Timestamp()
    {
        var now = DateTime.Now;
        // COB-TS layout: YYYY MM DD HH MIN SS MIL(2=hundredths); DB2-REST '0000'
        return now.ToString("yyyy-MM-dd-HH.mm.ss.") + (now.Millisecond / 10).ToString("D2") + "0000";
    }

    // ---- 9910 / 9999 -------------------------------------------------------------
    private void P9910_DisplayIoStatus() => Abend.DisplayIoStatus(_ioStatus);
    private void P9999_AbendProgram() => Abend.Cee3abd();
}
