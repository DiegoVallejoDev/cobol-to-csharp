using System;

namespace Legacy.Runtime;

/// <summary>
/// Zoned-decimal (USAGE DISPLAY, PIC S9(n)V9(m)) codec with the EBCDIC
/// overpunch sign convention used by the shipped data: the last byte carries
/// the sign — '{' or 'A'-'I' = positive, '}' or 'J'-'R' = negative.
/// Equivalent to GnuCOBOL -fsign=EBCDIC in an ASCII environment.
/// </summary>
public static class ZonedDecimal
{
    /// <summary>Decode a field of S9(n)V9(scale) into a decimal.</summary>
    public static decimal Parse(ReadOnlySpan<byte> field, int scale)
    {
        long digits = 0;
        bool negative = false;
        int last = field.Length - 1;
        for (int i = 0; i < last; i++)
        {
            byte b = field[i];
            if (b is >= (byte)'0' and <= (byte)'9')
                digits = digits * 10 + (b - (byte)'0');
        }
        byte lb = field[last];
        int lastDigit;
        if (lb is >= (byte)'0' and <= (byte)'9') { lastDigit = lb - (byte)'0'; }
        else if (lb == (byte)'{') { lastDigit = 0; }
        else if (lb == (byte)'}') { lastDigit = 0; negative = true; }
        else if (lb is >= (byte)'A' and <= (byte)'I') { lastDigit = lb - (byte)'A' + 1; }
        else if (lb is >= (byte)'J' and <= (byte)'R') { lastDigit = lb - (byte)'J' + 1; negative = true; }
        else { lastDigit = 0; }
        digits = digits * 10 + lastDigit;
        decimal v = digits;
        for (int i = 0; i < scale; i++) v /= 10m;
        return negative ? -v : v;
    }

    /// <summary>Encode a decimal into a field of S9(n)V9(scale) with overpunch sign.</summary>
    public static void Write(Span<byte> field, decimal value, int scale)
    {
        bool negative = value < 0;
        decimal abs = decimal.Abs(value);
        long scaleMul = 1;
        for (int i = 0; i < scale; i++) scaleMul *= 10;
        long digits = (long)decimal.Truncate(abs * scaleMul + 0.0000001m);
        int w = field.Length;
        for (int i = w - 1; i >= 0; i--)
        {
            field[i] = (byte)((byte)'0' + digits % 10);
            digits /= 10;
        }
        byte lb = field[w - 1];
        int lastDigit = lb - (byte)'0';
        field[w - 1] = negative
            ? (lastDigit == 0 ? (byte)'}' : (byte)((byte)'J' + lastDigit - 1))
            : (lastDigit == 0 ? (byte)'{' : (byte)((byte)'A' + lastDigit - 1));
    }

    /// <summary>
    /// How GnuCOBOL renders a signed DISPLAY-usage field in a DISPLAY
    /// statement: magnitude digits (implied decimal not shown) followed by
    /// an explicit '+'/'-' sign, e.g. '000000019400+'.
    /// </summary>
    public static string ToDisplayString(ReadOnlySpan<byte> field, int scale)
    {
        decimal v = Parse(field, scale);
        long scaleMul = 1;
        for (int i = 0; i < scale; i++) scaleMul *= 10;
        long digits = (long)decimal.Truncate(decimal.Abs(v) * scaleMul + 0.0000001m);
        return digits.ToString($"D{field.Length}") + (v < 0 ? "-" : "+");
    }
}
