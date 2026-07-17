using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DbfDataReader;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Services;

public class DbfFirmaDto
{
    public string FolderPath { get; set; } = "";
    public string Naziv { get; set; } = "";
    public string Pib { get; set; } = "";
    public string Mb { get; set; } = "";
    public string Mesto { get; set; } = "";
}

public class DbfImportService
{
    public static readonly DbfImportService Instance = new();

    private DbfImportService() 
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public List<DbfFirmaDto> UcitajFirme(string mainFolderPath)
    {
        var result = new List<DbfFirmaDto>();
        var korisnicPath = Path.Combine(mainFolderPath, "KORISNIC.DBF");

        if (!File.Exists(korisnicPath))
            throw new FileNotFoundException($"Fajl nije pronađen: {korisnicPath}");

        var encoding = Encoding.GetEncoding(852);
        var opts = new DbfDataReaderOptions { Encoding = encoding };
        
        using var reader = new DbfDataReader.DbfDataReader(korisnicPath, opts);
        var columns = GetColumns(reader);

        bool foundAny = false;
        while (reader.Read())
        {
            foundAny = true;
            string sifra = GetStringSafe(reader, columns, "KOR", "SIFRA");
            string naziv = GetStringSafe(reader, columns, "IME", "NAZIV");
            string pib = GetStringSafe(reader, columns, "PIB");
            string mb = GetStringSafe(reader, columns, "MB", "MATICNI");
            string grad = GetStringSafe(reader, columns, "GRAD", "MESTO");
            
            if (!string.IsNullOrEmpty(sifra))
            {
                var folderName = "KOR" + sifra;
                var folderPath = Path.Combine(mainFolderPath, folderName);

                if (Directory.Exists(folderPath))
                {
                    result.Add(new DbfFirmaDto
                    {
                        FolderPath = folderPath,
                        Naziv = string.IsNullOrWhiteSpace(naziv) ? folderName : naziv,
                        Pib = pib,
                        Mb = mb,
                        Mesto = grad
                    });
                }
            }
        }

        if (!foundAny)
        {
            var colNames = string.Join(", ", columns.Keys);
            throw new Exception($"Tabela KORISNIC.DBF je otvorena, ali nije pronađena nijedna firma.\nDbfDataReader vidi kolone: {colNames}");
        }

        return result;
    }

    private Dictionary<string, int> GetColumns(DbfDataReader.DbfDataReader reader)
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            dict[reader.GetName(i)] = i;
        }
        return dict;
    }

    private string GetStringSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> columns, params string[] colNames)
    {
        foreach (var colName in colNames)
        {
            if (columns.TryGetValue(colName, out int idx))
            {
                var val = reader.GetValue(idx)?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val)) return val;
            }
        }
        return "";
    }

    private decimal GetDecimalSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> columns, params string[] colNames)
    {
        foreach (var colName in colNames)
        {
            if (columns.TryGetValue(colName, out int idx))
            {
                var val = reader.GetValue(idx);
                if (val != null && val != DBNull.Value)
                {
                    try { return Convert.ToDecimal(val); } catch { }
                }
            }
        }
        return 0m;
    }

    private int GetIntSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> columns, params string[] colNames)
    {
        foreach (var colName in colNames)
        {
            if (columns.TryGetValue(colName, out int idx))
            {
                var val = reader.GetValue(idx);
                if (val != null && val != DBNull.Value)
                {
                    try { return Convert.ToInt32(val); } catch { }
                }
            }
        }
        return 0;
    }

    private DateTime GetDateSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> columns, params string[] colNames)
    {
        foreach (var colName in colNames)
        {
            if (columns.TryGetValue(colName, out int idx))
            {
                var val = reader.GetValue(idx);
                if (val is DateTime dt && dt != DateTime.MinValue) return dt;
            }
        }
        return DateTime.Now;
    }

    private bool GetBoolSafe(DbfDataReader.DbfDataReader reader, Dictionary<string, int> columns, params string[] colNames)
    {
        foreach (var colName in colNames)
        {
            if (columns.TryGetValue(colName, out int idx))
            {
                var val = reader.GetValue(idx);
                if (val is bool b) return b;
                var str = val?.ToString()?.Trim().ToUpper();
                if (str == "T" || str == "Y" || str == "1" || str == "TRUE") return true;
            }
        }
        return false;
    }

    public string ImportFirma(DbfFirmaDto firma)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var bazeDir = Path.Combine(appData, "SredstvaApp", "Baze");
        if (!Directory.Exists(bazeDir)) Directory.CreateDirectory(bazeDir);

        var safeName = string.Concat(firma.Naziv.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
        if (string.IsNullOrEmpty(safeName)) safeName = "Firma";

        var dbFileName = $"firma_{firma.Pib}_{safeName}.db";
        var dbPathToSave = Path.Combine(bazeDir, dbFileName);

        if (File.Exists(dbPathToSave))
        {
            dbFileName = $"firma_{firma.Pib}_{safeName}_{DateTime.Now:HHmmss}.db";
            dbPathToSave = Path.Combine(bazeDir, dbFileName);
        }

        using var db = SredstvaDbContext.Create(dbPathToSave);
        db.Database.EnsureCreated();

        // 1. Kreiranje firme
        var f = new Firma
        {
            Naziv = firma.Naziv,
            PIB = firma.Pib,
            MaticniBroj = firma.Mb,
            Mesto = firma.Mesto
        };
        db.Firme.Add(f);
        db.SaveChanges();

        // 1.5. Uvoz KONTPLAN.DBF (Dobavljaci)
        ImportDobavljaci(db, firma.FolderPath);

        // 2. Uvoz SREDSTVA.DBF
        ImportSredstva(db, firma.FolderPath);

        // 3. Uvoz KARTICA.DBF
        ImportKartice(db, firma.FolderPath);

        // 4. Uvoz PRIJAVA.DBF
        ImportPrijave(db, firma.FolderPath);

        // 5. Uvoz RASHOD.DBF
        ImportRashodi(db, firma.FolderPath);

        return dbPathToSave;
    }

    private void ImportDobavljaci(SredstvaDbContext db, string folderPath)
    {
        var path = Path.Combine(folderPath, "KONTPLAN.DBF");
        if (!File.Exists(path)) return;

        var encoding = Encoding.GetEncoding(852);
        var opts = new DbfDataReaderOptions { Encoding = encoding };
        using var reader = new DbfDataReader.DbfDataReader(path, opts);
        var columns = GetColumns(reader);

        var batch = new List<Dobavljac>();
        while (reader.Read())
        {
            int konto = GetIntSafe(reader, columns, "KONTO");
            if (konto == 0) continue; // Skip ako nema sifre

            var dob = new Dobavljac
            {
                Konto = konto,
                OpisKonta = GetStringSafe(reader, columns, "OPIS_KONTA", "OPISKONTA"),
                UlicaIBroj = GetStringSafe(reader, columns, "ULICA_I_BR", "ULICAIBR"),
                MestoIBroj = GetStringSafe(reader, columns, "MESTO_I_BR", "MESTOIBR")
            };
            batch.Add(dob);
        }
        db.Dobavljaci.AddRange(batch);
        db.SaveChanges();
    }

    private void ImportSredstva(SredstvaDbContext db, string folderPath)
    {
        var path = Path.Combine(folderPath, "SREDSTVA.DBF");
        if (!File.Exists(path)) return;

        var encoding = Encoding.GetEncoding(852);
        var opts = new DbfDataReaderOptions { Encoding = encoding };
        using var reader = new DbfDataReader.DbfDataReader(path, opts);
        var columns = GetColumns(reader);

        while (reader.Read())
        {
            var sr = new Sredstvo
            {
                JeAktivno = true,
                DatumNabavke = DateTime.Now,
                DatumAktiviranja = DateTime.Now
            };

            sr.LegacySifra = GetIntSafe(reader, columns, "SIFRA");
            sr.InventarskiBroj = GetStringSafe(reader, columns, "INVEN_BR", "INVBROJ");
            if (string.IsNullOrEmpty(sr.InventarskiBroj)) sr.InventarskiBroj = sr.LegacySifra.ToString();
            
            sr.Naziv = GetStringSafe(reader, columns, "NAZIV");
            sr.NabavnaVrednost = GetDecimalSafe(reader, columns, "NABAVNA", "NABVRED");
            sr.IspravkaVrednosti = GetDecimalSafe(reader, columns, "OTPISANA", "ISPRVRED");
            sr.StopaAmortizacije = GetDecimalSafe(reader, columns, "STOPA_AM");
            
            sr.Kolicina = GetDecimalSafe(reader, columns, "KOLICINA");
            if (sr.Kolicina == 0) sr.Kolicina = 1;
            
            sr.SadasnjaVrednost = sr.NabavnaVrednost - sr.IspravkaVrednosti;

            sr.DatumNabavke = GetDateSafe(reader, columns, "DAT_FAKTUR", "DAT_AKT", "DATUMNAB");
            sr.DatumAktiviranja = GetDateSafe(reader, columns, "DAT_AKT", "DATUMAKT");
            
            sr.Konto = GetStringSafe(reader, columns, "KONTO");
            sr.AmortizacionaGrupa = GetStringSafe(reader, columns, "AMORT_GR1", "AMGRUPA");

            db.Sredstva.Add(sr);
        }
        db.SaveChanges();
    }

    private void ImportKartice(SredstvaDbContext db, string folderPath)
    {
        var path = Path.Combine(folderPath, "KARTICA.DBF");
        if (!File.Exists(path)) return;

        var sredstvaMap = db.Sredstva.ToDictionary(s => s.LegacySifra, s => s.Id);

        var encoding = Encoding.GetEncoding(852);
        var opts = new DbfDataReaderOptions { Encoding = encoding };
        using var reader = new DbfDataReader.DbfDataReader(path, opts);
        var columns = GetColumns(reader);

        var batch = new List<Kartica>();
        while (reader.Read())
        {
            int oldSifra = GetIntSafe(reader, columns, "SIFRA");
            if (sredstvaMap.TryGetValue(oldSifra, out int sredstvoId))
            {
                var k = new Kartica { SredstvoId = sredstvoId, Datum = DateTime.Now };
                
                k.RedBroj = GetIntSafe(reader, columns, "RED_BROJ", "RBR");
                k.Datum = GetDateSafe(reader, columns, "DATUM");
                k.OpisPromene = GetStringSafe(reader, columns, "OPIS_PROM", "OPIS");
                k.ObracunskaJedinica = GetIntSafe(reader, columns, "OBRAC_JED", "OJ");
                k.Konto = GetStringSafe(reader, columns, "KONTO");
                k.Kolicina = GetDecimalSafe(reader, columns, "KOLICINA");
                k.NabavnaVrednost = GetDecimalSafe(reader, columns, "NABAVNA", "NABVRED");
                k.IspravkaVrednosti = GetDecimalSafe(reader, columns, "OTPISANA", "ISPRVRED");
                k.StopaAmortizacije = GetDecimalSafe(reader, columns, "STOPA_AM");
                k.KoeficijentRevalorizacije = GetDecimalSafe(reader, columns, "KOEFIC_REV");

                batch.Add(k);
            }
        }
        db.Kartice.AddRange(batch);
        db.SaveChanges();
    }

    private void ImportPrijave(SredstvaDbContext db, string folderPath)
    {
        var path = Path.Combine(folderPath, "PRIJAVA.DBF");
        if (!File.Exists(path)) return;

        var sredstvaMap = db.Sredstva.ToDictionary(s => s.LegacySifra, s => s.Id);
        var dobavljaciMap = db.Dobavljaci.ToDictionary(d => d.Konto, d => d.Id);

        var encoding = Encoding.GetEncoding(852);
        var opts = new DbfDataReaderOptions { Encoding = encoding };
        using var reader = new DbfDataReader.DbfDataReader(path, opts);
        var columns = GetColumns(reader);

        var batch = new List<Prijava>();
        while (reader.Read())
        {
            int oldSifra = GetIntSafe(reader, columns, "SIFRA");
            if (sredstvaMap.TryGetValue(oldSifra, out int sredstvoId))
            {
                var p = new Prijava { SredstvoId = sredstvoId, DatumAktiviranja = DateTime.Now };
                
                p.BrojNaloga = GetIntSafe(reader, columns, "BR_NALOGA", "BRNALOGA");
                p.RedBroj = GetIntSafe(reader, columns, "RED_BROJ", "RBR");
                p.ObracunskaJedinica = GetIntSafe(reader, columns, "OBRAC_JED", "OJ");
                p.Konto = GetStringSafe(reader, columns, "KONTO");
                p.DatumAktiviranja = GetDateSafe(reader, columns, "DAT_AKT", "DATUMAKT");
                p.NabavnaVrednost = GetDecimalSafe(reader, columns, "NABAVNA", "NABVRED");
                p.OtpisanaVrednost = GetDecimalSafe(reader, columns, "OTPISANA", "ISPRVRED");
                p.Kolicina = GetDecimalSafe(reader, columns, "KOLICINA");
                p.InventarskiBroj = GetStringSafe(reader, columns, "INVEN_BR", "INVBROJ");
                p.BrojFakture = GetStringSafe(reader, columns, "BR_FAKTURE", "BRFAK");
                p.DatumFakture = GetDateSafe(reader, columns, "DAT_FAKTUR", "DAT_FAKTURE");
                
                int dobKonto = GetIntSafe(reader, columns, "DOBAVLJAC");
                if (dobKonto > 0 && dobavljaciMap.TryGetValue(dobKonto, out int dobId))
                {
                    p.DobavljacId = dobId;
                }
                
                p.Knjizen = GetBoolSafe(reader, columns, "KNJIZEN");

                batch.Add(p);
            }
        }
        db.Prijave.AddRange(batch);
        db.SaveChanges();
    }

    private void ImportRashodi(SredstvaDbContext db, string folderPath)
    {
        var path = Path.Combine(folderPath, "RASHOD.DBF");
        if (!File.Exists(path)) return;

        var sredstvaMap = db.Sredstva.ToDictionary(s => s.LegacySifra, s => s.Id);

        var encoding = Encoding.GetEncoding(852);
        var opts = new DbfDataReaderOptions { Encoding = encoding };
        using var reader = new DbfDataReader.DbfDataReader(path, opts);
        var columns = GetColumns(reader);

        var batch = new List<Rashod>();
        while (reader.Read())
        {
            int oldSifra = GetIntSafe(reader, columns, "SIFRA");
            if (sredstvaMap.TryGetValue(oldSifra, out int sredstvoId))
            {
                var r = new Rashod { SredstvoId = sredstvoId, Datum = DateTime.Now, Kod = TipoviPromena.Rashodovanje };
                
                r.BrojNaloga = GetIntSafe(reader, columns, "BR_NALOGA", "BRNALOGA");
                r.RedBroj = GetIntSafe(reader, columns, "RED_BROJ", "RBR");
                r.Datum = GetDateSafe(reader, columns, "DATUM");
                r.DokumentBroj = GetStringSafe(reader, columns, "DOKUM_BROJ", "DOKBROJ");
                r.Podaci = GetDecimalSafe(reader, columns, "PODACI");
                r.ObracunskaJedinica = GetIntSafe(reader, columns, "OBRAC_JED", "OJ");
                r.Knjizen = GetBoolSafe(reader, columns, "KNJIZEN");
                r.KodTekst = GetStringSafe(reader, columns, "KOD_TEXT", "KODTEKST");
                
                int kodInt = GetIntSafe(reader, columns, "KOD");
                if (Enum.IsDefined(typeof(TipoviPromena), kodInt))
                    r.Kod = (TipoviPromena)kodInt;

                batch.Add(r);
            }
        }
        db.Rashodi.AddRange(batch);
        db.SaveChanges();
    }
}
