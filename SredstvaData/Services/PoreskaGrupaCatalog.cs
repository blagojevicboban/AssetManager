using System;
using System.Collections.Generic;
using System.Linq;

namespace SredstvaData.Services;

public record PoreskaGrupaInfo(string Kod, decimal Stopa, string Naziv, string Opis);

public static class PoreskaGrupaCatalog
{
    public static readonly List<PoreskaGrupaInfo> Grupe = new()
    {
        new("I", 2.5m, "Grupa I (2,5%)", "Nepokretnosti, zgrade, putevi, piste, parking površine"),
        new("II", 10.0m, "Grupa II (10,0%)", "Automobili, kancelarijska oprema, liftovi, nematerijalna ulaganja"),
        new("III", 15.0m, "Grupa III (15,0%)", "Alat, inventar, kamioni, autobusi, ostala nespecificirana sredstva"),
        new("IV", 20.0m, "Grupa IV (20,0%)", "Radio/TV oprema za emitovanje, naftne bušotine"),
        new("V", 30.0m, "Grupa V (30,0%)", "Računari, softveri, traktori, građevinska oprema, bilbordi")
    };

    public static decimal GetStopaZaGrupu(string kod)
    {
        var info = Grupe.FirstOrDefault(g => string.Equals(g.Kod, kod, StringComparison.OrdinalIgnoreCase));
        return info?.Stopa ?? 0m;
    }

    /// <summary>
    /// Pametni asistent koji na osnovu konta ili naziva sredstva predlaže najverovatniju poresku grupu.
    /// </summary>
    public static PoreskaGrupaInfo PredloziGrupu(string konto, string naziv)
    {
        naziv = (naziv ?? "").ToLower();
        konto = (konto ?? "").Trim();

        // 1. Zgrade / Nepokretnosti (Grupa I - 2.5%)
        if (konto.StartsWith("022") || naziv.Contains("zgrad") || naziv.Contains("objekat") || naziv.Contains("hala") || naziv.Contains("stan"))
        {
            return Grupe[0]; // Grupa I
        }

        // 2. Kompjuteri, softver, IT (Grupa V - 30%)
        if (naziv.Contains("racunar") || naziv.Contains("računar") || naziv.Contains("komp") || naziv.Contains("softver") || naziv.Contains("laptop") || naziv.Contains("server") || naziv.Contains("mobiln") || naziv.Contains("telefon"))
        {
            return Grupe[4]; // Grupa V
        }

        // 3. Automobili, kancelarijska oprema (Grupa II - 10%)
        if (naziv.Contains("auto") || naziv.Contains("vozil") || naziv.Contains("klima") || naziv.Contains("lift") || naziv.Contains("licenc") || naziv.Contains("paten"))
        {
            return Grupe[1]; // Grupa II
        }

        // 4. Podrazumevano: Grupa III (15%) prema Članu 2. st. 2. Pravilnika ("sva ostala stalna sredstva")
        return Grupe[2]; // Grupa III
    }
}
