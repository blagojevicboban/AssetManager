using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DbfDataReader;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
var enc = Encoding.GetEncoding(852);
var opts = new DbfDataReaderOptions { Encoding = enc };
var kor28 = @"C:\SREDSTVA\SREDS\KOR28\";

// ── Brisanje stare baze i pocetak ispocetka ──────────────────────────────────
using var db = new SredstvaDbContext();
var dbFile = db.DbPath;
if (File.Exists(dbFile))
{
    File.Delete(dbFile);
    Console.WriteLine("Stara baza obrisana.");
}
db.Database.EnsureCreated();
Console.WriteLine($"Nova baza kreirana: {dbFile}\n");

// ── 1. FIRMA ─────────────────────────────────────────────────────────────────
var firma = new Firma { Naziv = "KOR28 - Osnovna Sredstva", Mesto = "Novi Sad" };
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
        var konto = ToInt(r.GetValue(cols["KONTO"]));
        var d = new Dobavljac
        {
            Konto = konto,
            OpisKonta = Str(r.GetValue(cols["OPIS_KONTA"])),
            UlicaIBroj = Str(r.GetValue(cols["ULICA_I_BR"])),
            MestoIBroj = Str(r.GetValue(cols["MESTO_I_BR"]))
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
        var sifra = ToInt(r.GetValue(cols["SIFRA"]));
        var invenBr = Str(r.GetValue(cols["INVEN_BR"]));
        var nabavna = ToDec(r.GetValue(cols["NABAVNA"]));
        var otpisana = ToDec(r.GetValue(cols["OTPISANA"]));
        var s = new Sredstvo
        {
            LegacySifra = sifra,
            InventarskiBroj = string.IsNullOrWhiteSpace(invenBr) ? sifra.ToString() : invenBr,
            Naziv = Str(r.GetValue(cols["NAZIV"])),
            NabavnaVrednost = nabavna,
            IspravkaVrednosti = otpisana,
            SadasnjaVrednost = nabavna - otpisana,
            StopaAmortizacije = ToDec(r.GetValue(cols["STOPA_AM"])),
            AmortizacionaGrupa = ToInt(r.GetValue(cols["AMORT_GR1"])).ToString(),
            DatumAktiviranja = ToDate(r.GetValue(cols["DAT_AKT"])) ?? DateTime.MinValue,
            DatumNabavke = ToDate(r.GetValue(cols["DAT_AKT"])) ?? DateTime.MinValue,
            FirmaId = firma.Id,
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
        var sifra = ToInt(r.GetValue(cols["SIFRA"]));
        if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { karticeSkip++; continue; }
        var k = new Kartica
        {
            SredstvoId = sredstvoId,
            RedBroj = ToInt(r.GetValue(cols["RED_BROJ"])),
            Datum = ToDate(r.GetValue(cols["DATUM"])) ?? DateTime.MinValue,
            OpisPromene = Str(r.GetValue(cols["OPIS_PROM"])),
            ObracunskaJedinica = ToInt(r.GetValue(cols["OBRAC_JED"])),
            Konto = Str(r.GetValue(cols["KONTO"])),
            AmortizacionaGrupa1 = ToInt(r.GetValue(cols["AMORT_GR1"])),
            AmortizacionaGrupa2 = ToInt(r.GetValue(cols["AMORT_GR2"])),
            StopaAmortizacije = ToDec(r.GetValue(cols["STOPA_AM"])),
            KoeficijentRevalorizacije = ToDec(r.GetValue(cols["KOEFIC_REV"])),
            Kolicina = ToDec(r.GetValue(cols["KOLICINA"])),
            NabavnaVrednost = ToDec(r.GetValue(cols["NABAVNA"])),
            IspravkaVrednosti = ToDec(r.GetValue(cols["OTPISANA"]))
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
        var sifra = ToInt(r.GetValue(cols["SIFRA"]));
        if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { rashodiSkip++; continue; }
        var kodInt = ToInt(r.GetValue(cols["KOD"]));
        var rash = new Rashod
        {
            SredstvoId = sredstvoId,
            BrojNaloga = ToInt(r.GetValue(cols["BR_NALOGA"])),
            RedBroj = ToInt(r.GetValue(cols["RED_BROJ"])),
            Kod = Enum.IsDefined(typeof(TipoviPromena), kodInt) ? (TipoviPromena)kodInt : TipoviPromena.Rashodovanje,
            KodTekst = Str(r.GetValue(cols["KOD_TEXT"])),
            Datum = ToDate(r.GetValue(cols["DATUM"])) ?? DateTime.MinValue,
            DokumentBroj = Str(r.GetValue(cols["DOKUM_BROJ"])),
            Podaci = ToDec(r.GetValue(cols["PODACI"])),
            ObracunskaJedinica = ToInt(r.GetValue(cols["OBRAC_JED"])),
            Knjizen = ToInt(r.GetValue(cols["KNJIZEN"])) == 1
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
        var sifra = ToInt(r.GetValue(cols["SIFRA"]));
        // Ako je sifra prazna ili Sredstvo nije uvezeno, i dalje možemo uvesti Prijavu, 
        // ali EF očekuje validan SredstvoId. Zato ćemo preskočiti one bez sredstva.
        if (!sredstvaMap.TryGetValue(sifra, out var sredstvoId)) { prijaveSkip++; continue; }
        
        var p = new Prijava
        {
            SredstvoId = sredstvoId,
            BrojNaloga = ToInt(r.GetValue(cols["BR_NALOGA"])),
            RedBroj = ToInt(r.GetValue(cols["RED_BROJ"])),
            ObracunskaJedinica = ToInt(r.GetValue(cols["OBRAC_JED"])),
            Konto = Str(r.GetValue(cols["KONTO"])),
            AmortizacionaGrupa1 = ToInt(r.GetValue(cols["AMORT_GR1"])),
            AmortizacionaGrupa2 = ToInt(r.GetValue(cols["AMORT_GR2"])),
            StopaAmortizacije = ToDec(r.GetValue(cols["STOPA_AM"])),
            DatumAktiviranja = ToDate(r.GetValue(cols["DAT_AKT"])) ?? DateTime.MinValue,
            RevalorizacionaGrupa = ToInt(r.GetValue(cols["REVAL_GR"])),
            NabavnaVrednost = ToDec(r.GetValue(cols["NABAVNA"])),
            OtpisanaVrednost = ToDec(r.GetValue(cols["OTPISANA"])),
            JedinicaMere = Str(r.GetValue(cols["J_MERA"])),
            Kolicina = ToDec(r.GetValue(cols["KOLICINA"])),
            InventarskiBroj = Str(r.GetValue(cols["INVEN_BR"])),
            BrojFakture = Str(r.GetValue(cols["BR_FAKTURE"])),
            DatumFakture = ToDate(r.GetValue(cols["DAT_FAKTUR"])),
            BrojNalaznice = ToInt(r.GetValue(cols["BR_NALAZ"])),
            BrNal = Str(r.GetValue(cols["BR_NAL"])),
            GodNal = ToInt(r.GetValue(cols["GOD_NAL"])),
            Knjizen = ToInt(r.GetValue(cols["KNJIZEN"])) == 1
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
    var d = new Dictionary<string, int>();
    for (int i = 0; i < r.FieldCount; i++) d[r.GetName(i)] = i;
    return d;
}
static string Str(object? v) => v?.ToString()?.Trim() ?? string.Empty;
static int ToInt(object? v) { try { return Convert.ToInt32(v); } catch { return 0; } }
static decimal ToDec(object? v) { try { return Convert.ToDecimal(v); } catch { return 0m; } }
static DateTime? ToDate(object? v) { if (v is DateTime dt && dt != DateTime.MinValue) return dt; return null; }
