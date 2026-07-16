using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace SredstvaData;

/// <summary>
/// Fabrika za design-time kreiranje DbContext-a (potrebna za EF migracije).
/// Ne utice na runtime ponasanje aplikacije.
/// </summary>
public class SredstvaDbContextFactory : IDesignTimeDbContextFactory<SredstvaDbContext>
{
    public SredstvaDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(
            Path.GetTempPath(),
            "sredstva_migration_temp.db");

        var optionsBuilder = new DbContextOptionsBuilder<SredstvaDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new SredstvaDbContext(optionsBuilder.Options);
    }
}
