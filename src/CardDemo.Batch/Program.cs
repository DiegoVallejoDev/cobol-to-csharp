using System;
using CardDemo.Batch;

// Entry point mirroring EXEC PGM=CBACT04C,PARM='...'
// Usage: carddemo-batch <PROGRAM> <PARM> <DATA-DIR>
// DD assignments resolve to <data-dir>/<flat-file> per migration-map.
if (args.Length < 3)
{
    Console.Error.WriteLine("usage: carddemo-batch <PROGRAM> <PARM> <DATA-DIR>");
    return 8;
}

return args[0].ToUpperInvariant() switch
{
    "CBACT01C" => new Cbact01c(args[2]).Run(),
    "CBACT02C" => new Cbact02c(args[2]).Run(),
    "CBACT03C" => new Cbact03c(args[2]).Run(),
    "CBACT04C" => new Cbact04c(args[2], args[1]).Run(),
    var p => Unknown(p),
};

static int Unknown(string p)
{
    Console.Error.WriteLine($"unknown program {p}");
    return 8;
}
