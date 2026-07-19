using System.Text.RegularExpressions;
using SredstvaData.Models;

namespace SredstvaData.Services;

/// <summary>
/// Čista kalkulaciona logika za obračun amortizacije, izdvojena iz AmortizacijaPage
/// radi mogućnosti unit testiranja bez UI/DB zavisnosti.
/// </summary>
public static class AmortizacijaCalculator
{
    private static readonly Regex GodinaPattern = new(@"(?:Redovan otpis|Amortizacija)\s*\(?\b(\d{4})\b\)?", RegexOptions.Compiled);

    public record Rezultat(decimal NabavnaVrednost, decimal PrethodnaIspravka, decimal NovaAmortizacija);

    /// <summary>
    /// Obračunava proporcionalnu amortizaciju za jedno sredstvo u periodu [start, end],
    /// uzimajući u obzir sve promene (kartice) unutar perioda i ograničavajući rezultat
    /// na neotpisanu vrednost sredstva.
    /// </summary>
    public static Rezultat Izracunaj(decimal stopaAmortizacije, IEnumerable<Kartica> kartice, DateTime start, DateTime end)
    {
        var sveKartice = kartice.OrderBy(k => k.Datum).ToList();

        decimal tekucaNabavna = sveKartice.Where(k => k.Datum < start).Sum(k => k.NabavnaVrednost);
        decimal tekucaIspravka = sveKartice.Where(k => k.Datum < start).Sum(k => k.IspravkaVrednosti);

        decimal ukupnaNovaAmortizacija = 0;
        DateTime currentDate = start;
        decimal daniUGodini = DateTime.IsLeapYear(start.Year) ? 366m : 365m;

        var karticeUPeriodu = sveKartice.Where(k => k.Datum >= start && k.Datum <= end).ToList();

        foreach (var kartica in karticeUPeriodu)
        {
            int days = (kartica.Datum - currentDate).Days;
            if (days > 0 && tekucaNabavna > 0)
            {
                ukupnaNovaAmortizacija += (tekucaNabavna * (stopaAmortizacije / 100m)) * days / daniUGodini;
            }

            tekucaNabavna += kartica.NabavnaVrednost;
            tekucaIspravka += kartica.IspravkaVrednosti;
            currentDate = kartica.Datum;
        }

        int finalDays = (end - currentDate).Days + 1;
        if (finalDays > 0 && tekucaNabavna > 0)
        {
            ukupnaNovaAmortizacija += (tekucaNabavna * (stopaAmortizacije / 100m)) * finalDays / daniUGodini;
        }

        decimal neotpisanaVrednost = tekucaNabavna - tekucaIspravka;
        if (neotpisanaVrednost < 0) neotpisanaVrednost = 0;

        ukupnaNovaAmortizacija = Math.Min(ukupnaNovaAmortizacija, neotpisanaVrednost);

        return new Rezultat(tekucaNabavna, tekucaIspravka, Math.Round(ukupnaNovaAmortizacija, 2));
    }

    /// <summary>
    /// Parsira godinu obračuna iz opisa promene kartice (npr. "Amortizacija (2026)").
    /// </summary>
    public static bool TryParseGodina(string opisPromene, out int godina)
    {
        var match = GodinaPattern.Match(opisPromene);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int parsed))
        {
            godina = parsed;
            return true;
        }

        godina = 0;
        return false;
    }

    /// <summary>
    /// Suma svih ispravki vrednosti za sredstvo pre date kartice (hronološki, sa Id kao tie-breaker).
    /// </summary>
    public static decimal IzracunajPrethodnuIspravku(IEnumerable<Kartica> sveKarticeSredstva, Kartica kartica)
    {
        return sveKarticeSredstva
            .Where(k => k.Datum < kartica.Datum || (k.Datum == kartica.Datum && k.Id < kartica.Id))
            .Sum(k => k.IspravkaVrednosti);
    }
}
