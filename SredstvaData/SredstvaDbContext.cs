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
        
        ctx.Database.EnsureCreated(); // Kreira bazu ako ne postoji (sa svim tabelama + seed)
        
        // Sada proveravamo da li postoji migrations history tabela.
        // Ako ne postoji (baza je kreirana starim EnsureCreated kodom), 
        // dodajemo je i markiramo sve vec-primenjene migracije kao "Done".
        // Na taj nacin Migrate() ce primeniti SAMO nove migracije, bez pokusaja
        // kreiranja tabela koje vec postoje.
        var conn = ctx.Database.GetDbConnection();
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'";
            var historyExists = cmd.ExecuteScalar() != null;
            
            if (!historyExists)
            {
                // Kreiraj migrations history tabelu
                cmd.CommandText = @"CREATE TABLE __EFMigrationsHistory (
                    MigrationId TEXT NOT NULL CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY,
                    ProductVersion TEXT NOT NULL)";
                cmd.ExecuteNonQuery();
                
                // Markiraj pocetnu migraciju kao vec primenjenu (tabele su vec tu)
                cmd.CommandText = "INSERT INTO __EFMigrationsHistory VALUES ('20260715165530_AddKorisnici', '8.0.0')";
                cmd.ExecuteNonQuery();
            }
        }
        conn.Close();
        
        // Sada Migrate() ce primeniti samo NOVE migracije (npr. DodatiKontoObracunskaJedinica)
        ctx.Database.Migrate();
        return ctx;
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
            LozinkaHash = HashPassword("admin"), // Hardkodovani hash za "admin" za prvi login
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

    public static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
