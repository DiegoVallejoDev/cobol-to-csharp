namespace Legacy.Runtime.Tests;

public class ZonedDecimalTests
{
    [Fact]
    public void Write_Positive_OverpunchLastByte()
    {
        // Verified against GnuCOBOL golden: +2525.00 in S9(10)V99 → '00000025250{'
        var f = new byte[12];
        ZonedDecimal.Write(f, 2525.00m, 2);
        Assert.Equal("00000025250{", CobolFields.Latin1.GetString(f));
        Assert.Equal(2525.00m, ZonedDecimal.Parse(f, 2));
    }

    [Fact]
    public void Write_Negative_OverpunchLastByte()
    {
        // Verified: -1025.00 → '00000010250}'
        var f = new byte[12];
        ZonedDecimal.Write(f, -1025.00m, 2);
        Assert.Equal("00000010250}", CobolFields.Latin1.GetString(f));
        Assert.Equal(-1025.00m, ZonedDecimal.Parse(f, 2));
    }

    [Fact]
    public void Write_Zero_PositiveBrace()
    {
        var f = new byte[12];
        ZonedDecimal.Write(f, 0m, 2);
        Assert.Equal("00000000000{", CobolFields.Latin1.GetString(f));
    }
}

public class PackedDecimalTests
{
    [Fact]
    public void Write_Positive_SignNibbleLast()
    {
        // Verified against golden OUTFILE: +2525.00 in S9(10)V99 COMP-3 → 00 00 00 02 52 50 0C
        var f = new byte[7];
        PackedDecimal.Write(f, 2525.00m, 2);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x02, 0x52, 0x50, 0x0C }, f);
        Assert.Equal(2525.00m, PackedDecimal.Parse(f, 2));
    }

    [Fact]
    public void Write_Negative_SignNibbleD()
    {
        // Verified: -2500.00 → 00 00 00 02 50 00 0D
        var f = new byte[7];
        PackedDecimal.Write(f, -2500.00m, 2);
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x02, 0x50, 0x00, 0x0D }, f);
    }

    [Fact]
    public void RoundTrip() =>
        Assert.Equal(-42.17m, PackedDecimal.Parse(
            new byte[4].Also(f => PackedDecimal.Write(f, -42.17m, 2)), 2));
}

internal static class TExt
{
    public static T Also<T>(this T t, Action<T> act) { act(t); return t; }
}
