using System.Text.RegularExpressions;
using ERPiSredstvaData.Models;

namespace ERPiSredstvaData.Services;

public enum PocetakAmortizacijeRule
{
    SrazmernoDanima,
    OdNarednogMeseca
}

/// <summary>
/// Čista kalkulaciona logika za obračun amortizacije, izdvojena iz AmortizacijaPage
/// radi mogućnosti unit testiranja bez UI/DB zavisnosti.
/// </summary>
public static class AmortizacijaCalculator
{
    private static readonly Regex GodinaPattern = new(@"(?:Redovan otpis|Amortizacija)\s*\(?.*?\b(\d{4})\b\)?", RegexOptions.Compiled);

    public record Rezultat(decimal NabavnaVrednost, decimal PrethodnaIspravka, decimal NovaAmortizacija);

    /// <summary>
    /// Obračunava proporcionalnu amortizaciju za jedno sredstvo u periodu [start, end],
    /// uzimajući u obzir sve promene (kartice) unutar perioda i ograničavajući rezultat
    /// na neotpisanu vrednost sredstva.
    /// </summary>
    public static Rezultat Izracunaj(decimal stopaAmortizacije, IEnumerable<Kartica> kartice, DateTime start, DateTime end)
        => Izracunaj(stopaAmortizacije, kartice, start, end, rezidualnaVrednost: 0m, pocetakRule: PocetakAmortizacijeRule.SrazmernoDanima, datumAktiviranja: null);

    public static Rezultat Izracunaj(
        decimal stopaAmortizacije,
        IEnumerable<Kartica> kartice,
        DateTime start,
        DateTime end,
        decimal rezidualnaVrednost = 0m,
        PocetakAmortizacijeRule pocetakRule = PocetakAmortizacijeRule.SrazmernoDanima,
        DateTime? datumAktiviranja = null)
    {
        var sveKartice = kartice.OrderBy(k => k.Datum).ToList();

        decimal tekucaNabavna = sveKartice.Where(k => k.Datum < start).Sum(k => k.NabavnaVrednost);
        decimal tekucaIspravka = sveKartice.Where(k => k.Datum < start).Sum(k => k.IspravkaVrednosti);

        DateTime calcStartDate = start;

        if (datumAktiviranja.HasValue && datumAktiviranja.Value > start)
        {
            if (pocetakRule == PocetakAmortizacijeRule.OdNarednogMeseca)
            {
                calcStartDate = new DateTime(datumAktiviranja.Value.Year, datumAktiviranja.Value.Month, 1).AddMonths(1);
            }
            else
            {
                calcStartDate = datumAktiviranja.Value;
            }
        }

        if (calcStartDate > end)
        {
            return new Rezultat(tekucaNabavna, tekucaIspravka, 0m);
        }

        tekucaNabavna = sveKartice.Where(k => k.Datum < calcStartDate).Sum(k => k.NabavnaVrednost);
        tekucaIspravka = sveKartice.Where(k => k.Datum < calcStartDate).Sum(k => k.IspravkaVrednosti);

        decimal ukupnaNovaAmortizacija = 0;
        DateTime currentDate = calcStartDate;
        decimal daniUGodini = DateTime.IsLeapYear(start.Year) ? 366m : 365m;

        var karticeUPeriodu = sveKartice.Where(k => k.Datum >= calcStartDate && k.Datum <= end).ToList();

        foreach (var kartica in karticeUPeriodu)
        {
            int days = (kartica.Datum - currentDate).Days;
            if (days > 0)
            {
                decimal osnovica = Math.Max(0m, tekucaNabavna - rezidualnaVrednost);
                ukupnaNovaAmortizacija += (osnovica * (stopaAmortizacije / 100m)) * days / daniUGodini;
            }

            tekucaNabavna += kartica.NabavnaVrednost;
            tekucaIspravka += kartica.IspravkaVrednosti;
            currentDate = kartica.Datum;
        }

        int finalDays = (end - currentDate).Days + 1;
        if (finalDays > 0)
        {
            decimal osnovica = Math.Max(0m, tekucaNabavna - rezidualnaVrednost);
            ukupnaNovaAmortizacija += (osnovica * (stopaAmortizacije / 100m)) * finalDays / daniUGodini;
        }

        decimal amortizabilnaOsnovica = Math.Max(0m, tekucaNabavna - rezidualnaVrednost);
        decimal neotpisanaVrednost = Math.Max(0m, amortizabilnaOsnovica - tekucaIspravka);

        ukupnaNovaAmortizacija = Math.Min(ukupnaNovaAmortizacija, neotpisanaVrednost);

        return new Rezultat(tekucaNabavna, tekucaIspravka, Math.Round(ukupnaNovaAmortizacija, 2));
    }

    /// <summary>
    /// Generiše standardizovani opis promene u kartici na osnovu opsega perioda obračuna.
    /// (npr. "Amortizacija (2026)", "Amortizacija (03/2026)", "Amortizacija (Q1/2026)").
    /// </summary>
    public static string GenerisiOpisPromene(DateTime start, DateTime end)
    {
        if (start.Year == end.Year)
        {
            if (start.Month == 1 && start.Day == 1 && end.Month == 12 && end.Day == 31)
            {
                return $"Amortizacija ({start.Year})";
            }

            if (start.Month == end.Month && start.Day == 1 && end.Day == DateTime.DaysInMonth(start.Year, start.Month))
            {
                return $"Amortizacija ({start.Month:D2}/{start.Year})";
            }

            if (start.Day == 1)
            {
                if (start.Month == 1 && end.Month == 3 && end.Day == 31) return $"Amortizacija (Q1/{start.Year})";
                if (start.Month == 4 && end.Month == 6 && end.Day == 30) return $"Amortizacija (Q2/{start.Year})";
                if (start.Month == 7 && end.Month == 9 && end.Day == 30) return $"Amortizacija (Q3/{start.Year})";
                if (start.Month == 10 && end.Month == 12 && end.Day == 31) return $"Amortizacija (Q4/{start.Year})";
            }
        }

        return $"Amortizacija ({start:dd.MM.yyyy}-{end:dd.MM.yyyy})";
    }

    /// <summary>
    /// Parsira godinu obračuna iz opisa promene kartice (npr. "Amortizacija (2026)", "Amortizacija (03/2026)").
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
