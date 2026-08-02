using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DbfDataReader;
using Microsoft.EntityFrameworkCore;
using ERPiSredstvaData;
using ERPiSredstvaData.Models;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var enc = Encoding.GetEncoding(852);
var opts = new DbfDataReaderOptions { Encoding = enc };
var kor28 = @"C:\SREDSTVA\SREDS\KOR28\";

// Prvo citamo firmu da bismo znali ime baze
var firma = new Firma { Naziv = "KOR28 - Osnovna Sredstva" };
var korisnicDbf = @"C:\SREDSTVA\SREDS\KORISNIC.DBF";
if (File.Exists(korisnicDbf))
{
    using var rKor = new DbfDataReader.DbfDataReader(korisnicDbf, opts);
    var colsKor = GetCols(rKor);
    if (rKor.Read())
    {
        var ime = Str(GetSafe(rKor, colsKor, "IME"));
        if (!string.IsNullOrWhiteSpace(ime)) 
            firma.Naziv = ime;
            
        firma.Mesto = Str(GetSafe(rKor, colsKor, "GRAD"));
        if (string.IsNullOrWhiteSpace(firma.Mesto)) 
            firma.Mesto = Str(GetSafe(rKor, colsKor, "MESTO"));
            
        firma.PIB = Str(GetSafe(rKor, colsKor, "PIB"));
        firma.MaticniBroj = Str(GetSafe(rKor, colsKor, "MB"));
    }
}

var bazeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ERPiSredstvaApp", "Baze");
Directory.CreateDirectory(bazeDir);

var pib = !string.IsNullOrWhiteSpace(firma.PIB) ? firma.PIB.Trim() : firma.MaticniBroj?.Trim() ?? "UNKNOWN";
var nazivClean = string.Concat(firma.Naziv.Trim().Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
var dbFile = Path.Combine(bazeDir, $"firma_{pib}_{nazivClean}.db");

if (File.Exists(dbFile))
{
    File.Delete(dbFile);
    Console.WriteLine("Stara baza obrisana.");
}

using var db = SredstvaDbContext.Create(dbFile);
Console.WriteLine($"Nova baza kreirana: {dbFile}\n");
db.Firme.Add(firma);
db.SaveChanges();
Console.WriteLine($"[1/5] Firma kreirana (ID={firma.Id})");
// ── 2. DOBAVLJACI (KONTPLAN.DBF) ──────────────────────────────────────────────
var dobavljaciMap = new Dictionary<int, int>(); // konto -> db.Id
using (var r = new DbfDataReader.DbfDataReader(kor28 + "KONTPLAN.DBF", opts))
{
    var cols = GetCols(r);
    while (r.Read())
    {
        var konto = ToInt(GetSafe(r, cols, "KONTO"));
        var d = new Dobavljac
        {
            Konto = konto,
            OpisKonta = Str(GetSafe(r, cols, "OPIS_KONTA")),
            UlicaIBroj = Str(GetSafe(r, cols, "ULICA_I_BR")),
            MestoIBroj = Str(GetSafe(r, cols, "MESTO_I_BR"))
        };
        db.Dobavljaci.Add(d);
        db.SaveChanges();
        dobavljaciMap[konto] = d.Id;
    }
}
Console.WriteLine($"[2/5] Dobavljaci uvezeni: {dobavljaciMap.Count}");

// ── 3. SREDSTVA (SREDSTVA.DBF) ────────────────────────────────────────────────
var sredstvaMap = new Dictionary<int, int>(); // legacySifra -> db.Id
using (var r = new DbfDataReader.DbfDataReader(kor28 + "SREDSTVA.DBF", opts))
{
    var cols = GetCols(r);
    var batch = new List<Sredstvo>();
    while (r.Read())
    {
        var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
        var invenBr = Str(GetSafe(r, cols, "INVEN_BR"));
        var nabavna = ToDec(GetSafe(r, cols, "NABAVNA"));
        var otpisana = ToDec(GetSafe(r, cols, "OTPISANA"));
        var s = new Sredstvo
        {
            LegacySifra = sifra,
            InventarskiBroj = string.IsNullOrWhiteSpace(invenBr) ? sifra.ToString() : invenBr,
            Naziv = Str(GetSafe(r, cols, "NAZIV")),
            NabavnaVrednost = nabavna,
            IspravkaVrednosti = otpisana,
            SadasnjaVrednost = nabavna - otpisana,
            StopaAmortizacije = ToDec(GetSafe(r, cols, "STOPA_AM")),
            AmortizacionaGrupa = ToInt(GetSafe(r, cols, "AMORT_GR1")).ToString(),
            DatumAktiviranja = ToDate(GetSafe(r, cols, "DAT_AKT")) ?? DateTime.MinValue,
            DatumNabavke = ToDate(GetSafe(r, cols, "DAT_AKT")) ?? DateTime.MinValue,
            JeAktivno = true
        };
        batch.Add(s);
    }
    db.Sredstva.AddRange(batch);
    db.SaveChanges();
    // Build map after save (IDs are assigned)
    foreach (var s in batch)
        sredstvaMap[s.LegacySifra] = s.Id;
}
Console.WriteLine($"[3/5] Sredstva uvezena: {sredstvaMap.Count}");

// ── 4. KARTICE (KARTICA.DBF) ──────────────────────────────────────────────────
int karticeCount = 0, karticeSkip = 0;
var karticeBatch = new List<Kartica>();
using (var r = new DbfDataReader.DbfDataReader(kor28 + "KARTICA.DBF", opts))
{
    var cols = GetCols(r);
    while (r.Read())
    {
        var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
        if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { karticeSkip++; continue; }
        var k = new Kartica
        {
            SredstvoId = sredstvoId,
            RedBroj = ToInt(GetSafe(r, cols, "RED_BROJ")),
            Datum = ToDate(GetSafe(r, cols, "DATUM")) ?? DateTime.MinValue,
            OpisPromene = Str(GetSafe(r, cols, "OPIS_PROM")),
            ObracunskaJedinica = ToInt(GetSafe(r, cols, "OBRAC_JED")),
            Konto = Str(GetSafe(r, cols, "KONTO")),
            AmortizacionaGrupa1 = ToInt(GetSafe(r, cols, "AMORT_GR1")),
            AmortizacionaGrupa2 = ToInt(GetSafe(r, cols, "AMORT_GR2")),
            StopaAmortizacije = ToDec(GetSafe(r, cols, "STOPA_AM")),
            KoeficijentRevalorizacije = ToDec(GetSafe(r, cols, "KOEFIC_REV")),
            Kolicina = ToDec(GetSafe(r, cols, "KOLICINA")),
            NabavnaVrednost = ToDec(GetSafe(r, cols, "NABAVNA")),
            IspravkaVrednosti = ToDec(GetSafe(r, cols, "OTPISANA"))
        };
        karticeBatch.Add(k);
        karticeCount++;
    }
}
db.Kartice.AddRange(karticeBatch);
db.SaveChanges();
Console.WriteLine($"[4/5] Kartice uvezene: {karticeCount} (preskoceno: {karticeSkip})");

// ── 5. RASHODI (RASHOD.DBF) ───────────────────────────────────────────────────
int rashodiCount = 0, rashodiSkip = 0;
var rashodiBatch = new List<Rashod>();
using (var r = new DbfDataReader.DbfDataReader(kor28 + "RASHOD.DBF", opts))
{
    var cols = GetCols(r);
    while (r.Read())
    {
        var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
        if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { rashodiSkip++; continue; }
        var kodInt = ToInt(GetSafe(r, cols, "KOD"));
        var rash = new Rashod
        {
            SredstvoId = sredstvoId,
            BrojNaloga = ToInt(GetSafe(r, cols, "BR_NALOGA")),
            RedBroj = ToInt(GetSafe(r, cols, "RED_BROJ")),
            Kod = Enum.IsDefined(typeof(TipoviPromena), kodInt) ? (TipoviPromena)kodInt : TipoviPromena.Rashodovanje,
            KodTekst = Str(GetSafe(r, cols, "KOD_TEXT")),
            Datum = ToDate(GetSafe(r, cols, "DATUM")) ?? DateTime.MinValue,
            DokumentBroj = Str(GetSafe(r, cols, "DOKUM_BROJ")),
            Podaci = ToDec(GetSafe(r, cols, "PODACI")),
            ObracunskaJedinica = ToInt(GetSafe(r, cols, "OBRAC_JED")),
            Knjizen = ToInt(GetSafe(r, cols, "KNJIZEN")) == 1
        };
        rashodiBatch.Add(rash);
        rashodiCount++;
    }
}
db.Rashodi.AddRange(rashodiBatch);
db.SaveChanges();
Console.WriteLine($"[5/5] Rashodi uvezeni: {rashodiCount} (preskoceno: {rashodiSkip})");

// ── 6. PRIJAVE (PRIJAVA.DBF) ────────────────────────────────────────────────────
int prijaveCount = 0, prijaveSkip = 0;
var prijaveBatch = new List<Prijava>();
using (var r = new DbfDataReader.DbfDataReader(kor28 + "PRIJAVA.DBF", opts))
{
    var cols = GetCols(r);
    while (r.Read())
    {
        var sifra = ToInt(GetSafe(r, cols, "SIFRA"));
        // Ako je sifra prazna ili Sredstvo nije uvezeno, i dalje možemo uvesti Prijavu, 
        // ali EF očekuje validan SredstvoId. Zato ćemo preskočiti one bez sredstva.
        if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { prijaveSkip++; continue; }
        
        var p = new Prijava
        {
            SredstvoId = sredstvoId,
            BrojNaloga = ToInt(GetSafe(r, cols, "BR_NALOGA")),
            RedBroj = ToInt(GetSafe(r, cols, "RED_BROJ")),
            ObracunskaJedinica = ToInt(GetSafe(r, cols, "OBRAC_JED")),
            Konto = Str(GetSafe(r, cols, "KONTO")),
            AmortizacionaGrupa1 = ToInt(GetSafe(r, cols, "AMORT_GR1")),
            AmortizacionaGrupa2 = ToInt(GetSafe(r, cols, "AMORT_GR2")),
            StopaAmortizacije = ToDec(GetSafe(r, cols, "STOPA_AM")),
            DatumAktiviranja = ToDate(GetSafe(r, cols, "DAT_AKT")) ?? DateTime.MinValue,
            RevalorizacionaGrupa = ToInt(GetSafe(r, cols, "REVAL_GR")),
            NabavnaVrednost = ToDec(GetSafe(r, cols, "NABAVNA")),
            OtpisanaVrednost = ToDec(GetSafe(r, cols, "OTPISANA")),
            JedinicaMere = Str(GetSafe(r, cols, "J_MERA")),
            Kolicina = ToDec(GetSafe(r, cols, "KOLICINA")),
            InventarskiBroj = Str(GetSafe(r, cols, "INVEN_BR")),
            BrojFakture = Str(GetSafe(r, cols, "BR_FAKTURE")),
            DatumFakture = ToDate(GetSafe(r, cols, "DAT_FAKTUR")),
            BrojNalaznice = ToInt(GetSafe(r, cols, "BR_NALAZ")),
            BrNal = Str(GetSafe(r, cols, "BR_NAL")),
            GodNal = ToInt(GetSafe(r, cols, "GOD_NAL")),
            Knjizen = ToInt(GetSafe(r, cols, "KNJIZEN")) == 1
        };
        prijaveBatch.Add(p);
        prijaveCount++;
    }
}
db.Prijave.AddRange(prijaveBatch);
db.SaveChanges();
Console.WriteLine($"[6/6] Prijave uvezene: {prijaveCount} (preskoceno: {prijaveSkip})");

Console.WriteLine("\n✓ Kompletna migracija završena!");
// ── Helpers ───────────────────────────────────────────────────────────────────
static Dictionary<string, int> GetCols(DbfDataReader.DbfDataReader r)
{
    var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    for (int i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = i;
    return d;
}
static object? GetSafe(DbfDataReader.DbfDataReader r, Dictionary<string, int> cols, string key)
{
    if (cols.TryGetValue(key, out int idx)) return r.GetValue(idx);
    return null;
}
static string Str(object? v) => v?.ToString()?.Trim() ?? string.Empty;
static int ToInt(object? v) { try { return Convert.ToInt32(v); } catch { return 0; } }
static decimal ToDec(object? v) { try { return Convert.ToDecimal(v); } catch { return 0m; } }
static DateTime? ToDate(object? v) { if (v is DateTime dt && dt != DateTime.MinValue) return dt; return null; }
