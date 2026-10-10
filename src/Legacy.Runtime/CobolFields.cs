using System;
using System.Text;

namespace Legacy.Runtime;

/// <summary>
/// Byte-level field access over fixed-layout records — the C# analogue of
/// COBOL's byte-copy MOVE semantics (REDEFINES / group moves).
/// </summary>
public static class CobolFields
{
    public static readonly Encoding Latin1 = Encoding.Latin1;

    public static string GetString(byte[] rec, int offset, int length) =>
        Latin1.GetString(rec, offset, length);

    /// <summary>MOVE alphanumeric: left-align, space-pad, truncate to width.</summary>
    public static void SetString(byte[] rec, int offset, int length, string value)
    {
        var bytes = Latin1.GetBytes(value);
        int n = Math.Min(bytes.Length, length);
        Array.Copy(bytes, 0, rec, offset, n);
        for (int i = offset + n; i < offset + length; i++) rec[i] = (byte)' ';
    }

    public static void Fill(byte[] rec, int offset, int length, byte b)
    {
        for (int i = offset; i < offset + length; i++) rec[i] = b;
    }

    /// <summary>MOVE unsigned numeric: right-align, zero-pad.</summary>
    public static void SetUnsigned(byte[] rec, int offset, int length, long value)
    {
        var digits = value.ToString();
        int n = Math.Min(digits.Length, length);
        for (int i = offset; i < offset + length - n; i++) rec[i] = (byte)'0';
        Latin1.GetBytes(digits).CopyTo(rec, offset + length - n);
    }

    public static bool BytesEqual(byte[] a, int offA, byte[] b, int offB, int len)
    {
        for (int i = 0; i < len; i++) if (a[offA + i] != b[offB + i]) return false;
        return true;
    }
}
