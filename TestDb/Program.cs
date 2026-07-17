using System;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace TestDb
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "sredstva.db");
            if (!File.Exists(dbPath))
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SredstvaApp", "Baze");
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.db");
                    if (files.Length > 0) dbPath = files[0];
                }
            }

            Console.WriteLine($"DB Path: {dbPath}");
            using var db = SredstvaDbContext.Create(dbPath);

            var s77 = db.Sredstva.Include(s => s.Kartice).FirstOrDefault(s => s.LegacySifra == 77);
            if (s77 != null)
            {
                Console.WriteLine($"Sifra 77: Id={s77.Id}, JeAktivno={s77.JeAktivno}, Naziv={s77.Naziv}");
                foreach(var k in s77.Kartice.OrderBy(k => k.Datum))
                {
                    Console.WriteLine($"  Kartica: {k.Datum:yyyy-MM-dd} - {k.OpisPromene}");
                }
                var rashodi = db.Rashodi.Where(r => r.SredstvoId == s77.Id).ToList();
                foreach(var r in rashodi)
                {
                    Console.WriteLine($"  Rashod: {r.Datum:yyyy-MM-dd} - {r.KodTekst}");
                }
            }
            
            // Koliko sredstava ima Rashod a da je JeAktivno = true?
            var rashodovanaIds = db.Rashodi
                .Where(r => r.Kod == TipoviPromena.Rashodovanje || r.Kod == TipoviPromena.Prodaja || r.Kod == TipoviPromena.Otudjenje || r.Kod == TipoviPromena.Brisanje)
                .Select(r => r.SredstvoId)
                .Distinct()
                .ToList();
                
            var rashodovanaAktivna = db.Sredstva.Where(s => rashodovanaIds.Contains(s.Id) && s.JeAktivno).ToList();
            Console.WriteLine($"Broj sredstava koja imaju rashod a JeAktivno=true: {rashodovanaAktivna.Count}");
            
            // Koliko sredstava NEMA Rashod ali ima storniranje u kartici?
            var stornoIds = db.Kartice
                .Where(k => k.OpisPromene.StartsWith("Storniranje") || k.OpisPromene.Contains("rashod") || k.OpisPromene.Contains("Rashod"))
                .Select(k => k.SredstvoId)
                .Distinct()
                .ToList();
            
            var stornoAktivna = db.Sredstva.Where(s => stornoIds.Contains(s.Id) && s.JeAktivno).ToList();
            Console.WriteLine($"Broj sredstava koja imaju storniranje u kartici a JeAktivno=true: {stornoAktivna.Count}");
        }
    }
}
