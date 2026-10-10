using System;
using System.IO;
using Legacy.Runtime;

namespace CardDemo.Batch;

/// <summary>Faithful port of CBACT02C (READCARD job): sequential dump of CARDFILE.</summary>
public sealed class Cbact02c
{
    private readonly IndexedFile _card;
    private string _cardStatus = FileStatus.Ok;
    private string _ioStatus = "  ";
    private int _applResult;
    private char _endOfFile = 'N';
    private byte[] _cardRec = new byte[150];

    private bool ApplAok => _applResult == 0;
    private bool ApplEof => _applResult == 16;

    public Cbact02c(string dataDir)
        => _card = new IndexedFile(Path.Combine(dataDir, "carddata"), 150, 0, 16);

    public int Run()
    {
        Sysout.Display("START OF EXECUTION OF PROGRAM CBACT02C");
        P0000_CardfileOpen();
        while (_endOfFile != 'Y')
        {
            if (_endOfFile == 'N')
            {
                P1000_CardfileGetNext();
                if (_endOfFile == 'N')
                    Sysout.Display(_cardRec);
            }
        }
        P9000_CardfileClose();
        Sysout.Display("END OF EXECUTION OF PROGRAM CBACT02C");
        return 0;
    }

    private void P1000_CardfileGetNext()
    {
        _cardStatus = _card.ReadNext();
        if (_cardStatus == FileStatus.Ok) _applResult = 0;
        else if (_cardStatus == FileStatus.EndOfFile) _applResult = 16;
        else _applResult = 12;

        if (ApplAok) { _cardRec = _card.Current!; return; }
        if (ApplEof) { _endOfFile = 'Y'; return; }
        Sysout.Display("ERROR READING CARDFILE");
        _ioStatus = _cardStatus;
        P9910_DisplayIoStatus();
        P9999_AbendProgram();
    }

    private void P0000_CardfileOpen()
    {
        _applResult = 8;
        _cardStatus = _card.Open();
        _applResult = _cardStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR OPENING CARDFILE");
            _ioStatus = _cardStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P9000_CardfileClose()
    {
        _applResult = 8;
        _cardStatus = _card.Close();
        _applResult = _cardStatus == FileStatus.Ok ? 0 : 12;
        if (!ApplAok)
        {
            Sysout.Display("ERROR CLOSING CARDFILE");
            _ioStatus = _cardStatus;
            P9910_DisplayIoStatus();
            P9999_AbendProgram();
        }
    }

    private void P9910_DisplayIoStatus() => Abend.DisplayIoStatus(_ioStatus);
    private void P9999_AbendProgram() => Abend.Cee3abd();
}
