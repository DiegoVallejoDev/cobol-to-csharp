using System;
using System.Collections.Generic;
using System.IO;

namespace Legacy.Runtime;

/// <summary>COBOL file-status codes the ported programs branch on.</summary>
public static class FileStatus
{
    public const string Ok = "00";
    public const string EndOfFile = "10";
    public const string RecordNotFound = "23";
    public const string DuplicateKey = "22";
    public const string PermanentError = "30";
}

/// <summary>Lexicographic byte-array ordering (VSAM key order).</summary>
public sealed class ByteArrayComparer : IComparer<byte[]>
{
    public static readonly ByteArrayComparer Instance = new();
    public int Compare(byte[]? x, byte[]? y)
    {
        int n = Math.Min(x!.Length, y!.Length);
        for (int i = 0; i < n; i++)
            if (x[i] != y[i]) return x[i] - y[i];
        return x.Length - y.Length;
    }
}

/// <summary>
/// KSDS-style indexed file backed by a flat data file in key order:
/// records are staged into a sorted map on open; OPEN I-O persists the map
/// back (in key order) on close, so REWRITE semantics hold byte-exact.
/// Also supports one declared alternate key index (WITH DUPLICATES tolerated).
/// </summary>
public sealed class IndexedFile
{
    private readonly string _path;
    private readonly int _recordLength;
    private readonly int _keyOffset;
    private readonly int _keyLength;
    private readonly int? _altKeyOffset;
    private readonly int _altKeyLength;
    private readonly bool _persist;
    private readonly SortedList<byte[], byte[]> _records = new(ByteArrayComparer.Instance);
    private Dictionary<string, byte[]>? _altIndex;
    private int _seqPosition = -1;

    /// <param name="persist">true for OPEN I-O (writes the map back on Close).</param>
    public IndexedFile(string path, int recordLength, int keyOffset, int keyLength,
                       int? altKeyOffset = null, int altKeyLength = 0, bool persist = false)
    {
        _path = path;
        _recordLength = recordLength;
        _keyOffset = keyOffset;
        _keyLength = keyLength;
        _altKeyOffset = altKeyOffset;
        _altKeyLength = altKeyLength;
        _persist = persist;
    }

    public byte[]? Current { get; private set; }

    public string Open()
    {
        if (!File.Exists(_path)) return "35";
        using var fs = File.OpenRead(_path);
        var buf = new byte[_recordLength];
        while (fs.Read(buf, 0, _recordLength) == _recordLength)
        {
            var rec = (byte[])buf.Clone();
            var key = KeyOf(rec);
            _records[key] = rec;
            if (_altKeyOffset is { } altOff)
            {
                _altIndex ??= new Dictionary<string, byte[]>();
                _altIndex[CobolFields.GetString(rec, altOff, _altKeyLength)] = rec;
            }
        }
        return FileStatus.Ok;
    }

    private byte[] KeyOf(byte[] rec)
    {
        var k = new byte[_keyLength];
        Array.Copy(rec, _keyOffset, k, 0, _keyLength);
        return k;
    }

    /// <summary>Sequential READ (ACCESS SEQUENTIAL) — next in key order.</summary>
    public string ReadNext()
    {
        _seqPosition++;
        if (_seqPosition >= _records.Count) { Current = null; return FileStatus.EndOfFile; }
        Current = _records.Values[_seqPosition];
        return FileStatus.Ok;
    }

    /// <summary>Keyed READ; alt=true resolves through the alternate index.</summary>
    public string ReadKey(ReadOnlySpan<byte> key, bool alt = false)
    {
        byte[]? rec = null;
        if (alt)
        {
            if (_altIndex is { } ix && ix.TryGetValue(CobolFields.Latin1.GetString(key), out var r)) rec = r;
        }
        else
        {
            _records.TryGetValue(key.ToArray(), out rec);
        }
        Current = rec;
        return rec is null ? FileStatus.RecordNotFound : FileStatus.Ok;
    }

    /// <summary>REWRITE the record with the given key.</summary>
    public string Rewrite(byte[] record)
    {
        var key = KeyOf(record);
        if (!_records.ContainsKey(key)) return FileStatus.RecordNotFound;
        _records[key] = record;
        if (_altIndex is { } ix && _altKeyOffset is { } altOff)
            ix[CobolFields.GetString(record, altOff, _altKeyLength)] = record;
        return FileStatus.Ok;
    }

    public string Close()
    {
        if (_persist && _path is not null)
        {
            using var fs = File.Create(_path);
            foreach (var rec in _records.Values) fs.Write(rec, 0, _recordLength);
        }
        return FileStatus.Ok;
    }
}

/// <summary>ORGANIZATION SEQUENTIAL writer — raw fixed-length records, no delimiters.</summary>
public sealed class SequentialFileWriter : IDisposable
{
    private readonly FileStream _fs;
    private readonly int _recordLength;

    public SequentialFileWriter(string path, int recordLength)
    {
        _recordLength = recordLength;
        _fs = File.Create(path);
    }

    public string Write(byte[] record)
    {
        _fs.Write(record, 0, _recordLength);
        return FileStatus.Ok;
    }

    public string Close() { _fs.Dispose(); return FileStatus.Ok; }
    public void Dispose() => _fs.Dispose();
}

/// <summary>
/// RECORD VARYING writer — each record is preceded by GnuCOBOL's 4-byte
/// RDW: 2-byte big-endian length + 2 reserved zero bytes.
/// </summary>
public sealed class VariableLengthFileWriter : IDisposable
{
    private readonly FileStream _fs;

    public VariableLengthFileWriter(string path) => _fs = File.Create(path);

    public string Write(byte[] record, int length)
    {
        // GnuCOBOL RDW: 2-byte BIG-endian length + 2 reserved zero bytes.
        _fs.WriteByte((byte)((length >> 8) & 0xFF));
        _fs.WriteByte((byte)(length & 0xFF));
        _fs.WriteByte(0);
        _fs.WriteByte(0);
        _fs.Write(record, 0, length);
        return FileStatus.Ok;
    }

    public string Close() { _fs.Dispose(); return FileStatus.Ok; }
    public void Dispose() => _fs.Dispose();
}
