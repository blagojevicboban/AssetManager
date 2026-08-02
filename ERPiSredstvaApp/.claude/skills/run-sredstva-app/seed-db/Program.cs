using ERPiSredstvaData;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: seed-db <path-to-new-db-file>");
    Environment.Exit(1);
}

var dbPath = args[0];
if (File.Exists(dbPath))
{
    Console.Error.WriteLine($"Refusing to overwrite existing file: {dbPath}");
    Environment.Exit(1);
}

using var db = SredstvaDbContext.Create(dbPath);
Console.WriteLine($"Created fresh scratch db (schema + seed admin/admin user): {dbPath}");
