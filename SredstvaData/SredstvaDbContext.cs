using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SredstvaData.Models;

namespace SredstvaData;

public class SredstvaDbContext : DbContext
{
    public DbSet<Firma> Firme { get; set; }
    public DbSet<Sredstvo> Sredstva { get; set; }
    public DbSet<Dobavljac> Dobavljaci { get; set; }
    public DbSet<Prijava> Prijave { get; set; }
    public DbSet<Kartica> Kartice { get; set; }
    public DbSet<Rashod> Rashodi { get; set; }
    
    public DbSet<Komisija> Komisije { get; set; }
    public DbSet<ClanKomisije> ClanoviKomisije { get; set; }
    public DbSet<Popis> Popisi { get; set; }
    public DbSet<PopisnaStavka> PopisneStavke { get; set; }
    
    public DbSet<Korisnik> Korisnici { get; set; }

    public string? DbPath { get; internal set; }

    public SredstvaDbContext()
    {
    }

    public static SredstvaDbContext Create(string dbPath)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SredstvaDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        var ctx = new SredstvaDbContext(optionsBuilder.Options);
        ctx.DbPath = dbPath;

        // Baze nastale pre uvodjenja EF Core migracija nemaju __EFMigrationsHistory
        // tabelu. Njih jednokratno "krstimo" na trenutno stanje sheme kako bi
        // Migrate() ispod mogao preuzeti sve buduce promene sheme na standardan,
        // ugradjen nacin (bez rucnog SQL patch-a za svaku narednu migraciju).
        BaselineLegacyDatabaseIfNeeded(dbPath);

        ctx.Database.Migrate();

        return ctx;
    }

    /// <summary>
    /// Dovodi bazu nastalu pre migracija (EnsureCreated era) u stanje koje odgovara
    /// prve dve migracije (AddKorisnici, DodatiKontoObracunskaJedinica), pa markira
    /// istoriju migracija kao izvrsenu. Ne dira baze koje vec imaju istoriju migracija
    /// - za njih Database.Migrate() radi sve normalno.
    /// </summary>
    private static void BaselineLegacyDatabaseIfNeeded(string dbPath)
    {
        if (!File.Exists(dbPath))
        {
            // Nova baza - Database.Migrate() ce je kreirati od nule kroz migracije.
            return;
        }

        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        bool TableExists(string table)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@t";
            cmd.Parameters.AddWithValue("@t", table);
            return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
        }

        if (TableExists("__EFMigrationsHistory"))
        {
            // Baza je vec na tragu migracija - nema sta da se radi ovde.
            return;
        }

        // Bezbednosna kopija pre bilo kakve izmene sheme (best-effort, ne blokira start).
        try
        {
            var backupPath = $"{dbPath}.pre-migracija-{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            File.Copy(dbPath, backupPath, overwrite: true);
        }
        catch
        {
        }

        bool ColumnExists(string table, string column)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{table}') WHERE name=@c";
            cmd.Parameters.AddWithValue("@c", column);
            return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
        }

        void Exec(string sql)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
        }

        Exec(@"CREATE TABLE IF NOT EXISTS __EFMigrationsHistory (
                MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                ProductVersion TEXT NOT NULL);");

        // 1. Korisnici tabela je uvedena tek u AddKorisnici migraciji. Baze koje
        //    postoje od pre uvodjenja prijave/uloga korisnika je nemaju - napravimo
        //    je sada, inace prva prijava puca sa "no such table: Korisnici".
        if (!TableExists("Korisnici"))
        {
            Exec(@"CREATE TABLE ""Korisnici"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Korisnici"" PRIMARY KEY AUTOINCREMENT,
                    ""ImePrezime"" TEXT NOT NULL,
                    ""KorisnickoIme"" TEXT NOT NULL,
                    ""LozinkaHash"" TEXT NOT NULL,
                    ""Uloga"" INTEGER NOT NULL,
                    ""JeAktivan"" INTEGER NOT NULL);");
            Exec(@"INSERT INTO ""Korisnici""
                    (""Id"",""ImePrezime"",""KorisnickoIme"",""LozinkaHash"",""Uloga"",""JeAktivan"")
                    VALUES (1,'Administrator','admin','" + HashPassword("admin") + @"',0,1);");
        }

        // 2. Sredstva.FirmaId -> ObracunskaJedinica (preimenovanje kolone iz stare seme)
        Exec("DROP INDEX IF EXISTS \"IX_Sredstva_FirmaId\";");

        if (!ColumnExists("Sredstva", "ObracunskaJedinica"))
        {
            if (ColumnExists("Sredstva", "FirmaId"))
            {
                Exec("ALTER TABLE \"Sredstva\" RENAME COLUMN \"FirmaId\" TO \"ObracunskaJedinica\";");
            }
            else
            {
                Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"ObracunskaJedinica\" INTEGER NOT NULL DEFAULT 0;");
            }
        }

        // 3. Konto kolona (dodata u DodatiKontoObracunskaJedinica migraciji)
        if (!ColumnExists("Sredstva", "Konto"))
        {
            Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"Konto\" TEXT NOT NULL DEFAULT '';");
        }

        if (!ColumnExists("Sredstva", "RezidualnaVrednost"))
        {
            Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"RezidualnaVrednost\" TEXT NOT NULL DEFAULT '0';");
        }

        if (!ColumnExists("Sredstva", "PoreskaGrupa"))
        {
            Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"PoreskaGrupa\" TEXT NOT NULL DEFAULT '';");
        }
        if (!ColumnExists("Sredstva", "PoreskaStopa"))
        {
            Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"PoreskaStopa\" TEXT NOT NULL DEFAULT '0';");
        }
        if (!ColumnExists("Sredstva", "PoreskaNabavnaVrednost"))
        {
            Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"PoreskaNabavnaVrednost\" TEXT NOT NULL DEFAULT '0';");
        }
        if (!ColumnExists("Sredstva", "PoreskaIspravkaVrednosti"))
        {
            Exec("ALTER TABLE \"Sredstva\" ADD COLUMN \"PoreskaIspravkaVrednosti\" TEXT NOT NULL DEFAULT '0';");
        }

        // 4. Shema sada odgovara stanju posle prve dve migracije - markiraj ih kao izvrsene
        //    da bi Database.Migrate() ispod primenio samo migracije koje dolaze POSLE ovih.
        Exec(@"INSERT OR IGNORE INTO __EFMigrationsHistory VALUES ('20260715165530_AddKorisnici', '8.0.0');
               INSERT OR IGNORE INTO __EFMigrationsHistory VALUES ('20260716093143_DodatiKontoObracunskaJedinica', '8.0.0');");
    }

    public SredstvaDbContext(DbContextOptions<SredstvaDbContext> options) : base(options)
    {
    }
    
    public SredstvaDbContext(string dbPath)
    {
        DbPath = dbPath;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(DbPath))
        {
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Seed default Admin
        modelBuilder.Entity<Korisnik>().HasData(new Korisnik
        {
            Id = 1,
            ImePrezime = "Administrator",
            KorisnickoIme = "admin",
            // Hardkodovani (fiksni) osoljeni PBKDF2 hash za "admin" za prvi login.
            // Mora biti konstanta, ne poziv HashPassword() - EF HasData zahteva
            // determinističku vrednost jer ulazi u model snapshot za migracije.
            LozinkaHash = "PBKDF2$100000$9HpsWOyoV9tk7boQMPu8Iw==$tKuZniNJrMWGpwsjSJQrN7wSaeHWIxO+c8lXgvB5hzY=",
            Uloga = UlogaKorisnika.Administrator,
            JeAktivan = true
        });
        

        modelBuilder.Entity<Kartica>()
            .HasOne(k => k.Sredstvo)
            .WithMany(s => s.Kartice)
            .HasForeignKey(k => k.SredstvoId);

        modelBuilder.Entity<Prijava>()
            .HasOne(p => p.Sredstvo)
            .WithMany(s => s.Prijave)
            .HasForeignKey(p => p.SredstvoId);

        modelBuilder.Entity<Prijava>()
            .HasOne(p => p.Dobavljac)
            .WithMany(d => d.Prijave)
            .HasForeignKey(p => p.DobavljacId)
            .IsRequired(false);

        modelBuilder.Entity<Rashod>()
            .HasOne(r => r.Sredstvo)
            .WithMany(s => s.Rashodi)
            .HasForeignKey(r => r.SredstvoId);

        modelBuilder.Entity<ClanKomisije>()
            .HasOne(c => c.Komisija)
            .WithMany(k => k.Clanovi)
            .HasForeignKey(c => c.KomisijaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Popis>()
            .HasOne(p => p.Komisija)
            .WithMany(k => k.Popisi)
            .HasForeignKey(p => p.KomisijaId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PopisnaStavka>()
            .HasOne(ps => ps.Popis)
            .WithMany(p => p.Stavke)
            .HasForeignKey(ps => ps.PopisId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PopisnaStavka>()
            .HasOne(ps => ps.Sredstvo)
            .WithMany()
            .HasForeignKey(ps => ps.SredstvoId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private const int PasswordSaltSize = 16;
    private const int PasswordHashSize = 32;
    private const int PasswordIterations = 100_000;

    public static string HashPassword(string password)
    {
        var salt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(PasswordSaltSize);
        var hash = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
            password, salt, PasswordIterations, System.Security.Cryptography.HashAlgorithmName.SHA256, PasswordHashSize);
        return $"PBKDF2${PasswordIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    // Podržava i stare, neosoljene SHA-256 heševe iz baza kreiranih pre uvođenja soli -
    // pri uspešnoj prijavi pozivalac treba da presnimi heš pozivom HashPassword.
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return false;

        if (storedHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
        {
            var parts = storedHash.Split('$');
            if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations)) return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                var actual = System.Security.Cryptography.Rfc2898DeriveBytes.Pbkdf2(
                    password, salt, iterations, System.Security.Cryptography.HashAlgorithmName.SHA256, expected.Length);
                return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        try
        {
            var legacyHash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            var legacyExpected = Convert.FromBase64String(storedHash);
            return legacyExpected.Length == legacyHash.Length
                && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(legacyHash, legacyExpected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
