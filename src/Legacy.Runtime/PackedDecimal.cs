using System;

namespace Legacy.Runtime;

/// <summary>
/// COMP-3 packed-decimal codec: digits packed two per byte in order, sign
/// nibble last — C = positive, D = negative (GnuCOBOL -fsign=EBCDIC
/// convention). Fields with an even digit count get a leading zero pad
/// nibble, so a S9(n)V9(m) field occupies floor((n+m)/2)+1 bytes.
/// </summary>
public static class PackedDecimal
{
    /// <summary>Bytes occupied by a signed field of <paramref name="digits"/> digits.</summary>
    public static int ByteLength(int digits) => digits / 2 + 1;

    public static decimal Parse(ReadOnlySpan<byte> field, int scale)
    {
        long digits = 0;
        for (int i = 0; i < field.Length - 1; i++)
            digits = digits * 100 + (field[i] >> 4) * 10 + (field[i] & 0x0F);
        byte last = field[^1];
        digits = digits * 10 + (last >> 4);
        bool negative = (last & 0x0F) == 0x0D;
        decimal v = digits;
        for (int i = 0; i < scale; i++) v /= 10m;
        return negative ? -v : v;
    }

    /// <summary>
    /// Encode a decimal into a field of digit-capacity (length*2-1) digits:
    /// value scaled to integer, left-padded, packed two digits per byte,
    /// final byte = last digit + sign nibble.
    /// </summary>
    public static void Write(Span<byte> field, decimal value, int scale)
    {
        bool negative = value < 0;
        decimal abs = decimal.Abs(value);
        long scaleMul = 1;
        for (int i = 0; i < scale; i++) scaleMul *= 10;
        long scaled = (long)decimal.Truncate(abs * scaleMul + 0.0000001m);
        var digits = scaled.ToString().PadLeft(field.Length * 2 - 1, '0');
        for (int i = 0; i < field.Length - 1; i++)
            field[i] = (byte)(((digits[2 * i] - '0') << 4) | (digits[2 * i + 1] - '0'));
        field[^1] = (byte)(((digits[^1] - '0') << 4) | (negative ? 0x0D : 0x0C));
    }
}
