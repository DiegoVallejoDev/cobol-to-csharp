using System;
using System.IO;

namespace Legacy.Runtime;

/// <summary>
/// SYSOUT channel — COBOL DISPLAY writes record bytes (which may contain
/// overpunch/zoned characters) verbatim plus a newline. Latin-1 keeps a
/// 1:1 byte mapping.
/// </summary>
public static class Sysout
{
    private static readonly TextWriter Out = Console.Out;

    public static void Display(string text) => Out.WriteLine(text);
    public static void Display(byte[] record) =>
        Out.WriteLine(CobolFields.Latin1.GetString(record));
}

/// <summary>
/// Emulates CALL 'CEE3ABD' — Language Environment abend. Displays the
/// message then terminates the run unit with a non-zero return code,
/// mirroring the GnuCOBOL stub used by the parity harness.
/// </summary>
public static class Abend
{
    public static void Cee3abd(int code = 999)
    {
        Sysout.Display("ABENDING PROGRAM");
        Sysout.Display($"CEE3ABD: ABEND CODE {code}");
        Environment.Exit(16);
    }

    /// <summary>9910-DISPLAY-IO-STATUS equivalent: FILE STATUS IS: NNNN &lt;decoded&gt;.</summary>
    public static void DisplayIoStatus(string ioStatus)
    {
        bool numeric = ioStatus.Length == 2 && char.IsDigit(ioStatus[0]) && char.IsDigit(ioStatus[1]);
        if (!numeric || ioStatus[0] == '9')
        {
            int stat2 = ioStatus.Length > 1 ? ioStatus[1] : 0;
            Sysout.Display($"FILE STATUS IS: NNNN {ioStatus[0]}{stat2:000}");
        }
        else
        {
            Sysout.Display($"FILE STATUS IS: NNNN 0{ioStatus}0");
        }
    }
}
