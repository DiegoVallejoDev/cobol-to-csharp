using System;
using System.IO;
using Legacy.Runtime;

namespace CardDemo.Batch;

/// <summary>Faithful port of CBACT03C (READXREF job): sequential dump of XREFFILE.</summary>
public sealed class Cbact03c
{
    private readonly IndexedFile _xref;
    private string _xrefStatus = FileStatus.Ok;
    private string _ioStatus = "  ";
    private int _applResult;
    private char _endOfFile = 'N';
    private byte[] _xrefRec = new byte[50];

    private bool ApplAok => _applResult == 0;
    private bool ApplEof => _applResult == 16;

    public Cbact03c(string dataDir)
        => _xref = new IndexedFile(Path.Combine(dataDir, "cardxref"), 50, 0, 16, altKeyOffset: 25, altKeyLength: 11);

    public int Run()
    {
        Sysout.Display("START OF EXECUTION OF PROGRAM CBACT03C");
        P0000_XrefFileOpen();
        while (_endOfFile != 'Y')
        {
            if (_endOfFile == 'N')
            {
                P1000_XrefFileGetNext();
                if (_endOfFile == 'N')
                    Sysout.Display(_xrefRec);
            }
        }
        P9000_XrefFileClose();
        Sysout.Display("END OF EXECUTION OF PROGRAM CBACT03C");
        return 0;
    }

    private void P1000_XrefFileGetNext()
    {
        _xrefStatus = _xref.ReadNext();
        if (_xrefStatus == FileStatus.Ok) _applResult = 0;
        else if (_xrefStatus == FileStatus.EndOfFile) _applResult = 16;
        else _applResult = 12;

        if (ApplAok)
        {
            _xrefRec = _xref.Current!;
            Sysout.Display(_xrefRec);   // DISPLAY inside read paragraph (line 96) — records print twice
            return;
        }
        if (ApplEof) { _endOfFile = 'Y'; return; }
        Sysout.Display("ERROR READING XREF FILE");
        _ioStatus = _xrefStatus;
        P9910_DisplayIoStatus();
        P9999_AbendProgram();
    }

    private void P0000_XrefFileOpen()
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

    private void P9000_XrefFileClose()
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

    private void P9910_DisplayIoStatus() => Abend.DisplayIoStatus(_ioStatus);
    private void P9999_AbendProgram() => Abend.Cee3abd();
}
