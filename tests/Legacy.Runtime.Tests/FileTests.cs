namespace Legacy.Runtime.Tests;

public class FileTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"lrt-{Guid.NewGuid():N}");

    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    [Fact]
    public void IndexedFile_KeyOrderReadKeyAndMiss()
    {
        var path = Path.Combine(_dir, "acct");
        Directory.CreateDirectory(_dir);
        // three 300B records, inserted out of key order
        var flat = new byte[900];
        foreach (var (id, i) in new[] { "000000000003", "000000000001", "000000000002" }.Select((x, i) => (x, i)))
        {
            var rec = new byte[300];
            CobolFields.SetString(rec, 0, 12, id);
            CobolFields.SetString(rec, 12, 10, $"name-{id[^1]}");
            rec.CopyTo(flat, i * 300);
        }
        File.WriteAllBytes(path, flat);

        var f = new IndexedFile(path, 300, 0, 12);
        Assert.Equal("00", f.Open());
        Assert.Equal("00", f.ReadNext()); // key order, not insertion order
        Assert.Equal("000000000001", CobolFields.GetString(f.Current!, 0, 12));
        var key = CobolFields.Latin1.GetBytes("000000000003");
        Assert.Equal("00", f.ReadKey(key));
        Assert.Equal("name-3", CobolFields.GetString(f.Current!, 12, 6));
        Assert.Equal("23", f.ReadKey(CobolFields.Latin1.GetBytes("999999999999")));
        Assert.Null(f.Current);
    }

    [Fact]
    public void IndexedFile_RewritePersistsOnClose()
    {
        var path = Path.Combine(_dir, "acct2");
        Directory.CreateDirectory(_dir);
        var rec = new byte[50];
        CobolFields.SetString(rec, 0, 12, "000000000007");
        File.WriteAllBytes(path, rec);

        var f = new IndexedFile(path, 50, 0, 12, persist: true);
        f.Open();
        CobolFields.SetString(rec, 12, 5, "HELLO");
        Assert.Equal("00", f.Rewrite(rec));
        f.Close();
        Assert.Equal("HELLO", CobolFields.Latin1.GetString(File.ReadAllBytes(path)[12..17]));
    }

    [Fact]
    public void VariableLengthFileWriter_BigEndianRdw()
    {
        // Verified against GnuCOBOL VBRCFILE golden: 4-byte RDW = 2-byte BIG-endian length + 2 nul
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "vbrc");
        var rec = new byte[12];
        CobolFields.SetString(rec, 0, 12, "012345678901");
        using (var w = new VariableLengthFileWriter(path)) w.Write(rec, rec.Length);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(16, bytes.Length);
        Assert.Equal(new byte[] { 0x00, 0x0C, 0x00, 0x00 }, bytes[..4]);
        Assert.Equal(rec, bytes[4..]);
    }
}
