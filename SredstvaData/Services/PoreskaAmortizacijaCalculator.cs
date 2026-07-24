using System;
using System.Collections.Generic;
using System.Linq;
using SredstvaData.Models;

namespace SredstvaData.Services;

/// <summary>
/// Logika za obračun poreske amortizacije u skladu sa Pravilnikom o poreskoj amortizaciji
/// (Obrazac OA za sredstva stvorena/nabavljena od 1. januara 2019. godine).
/// </summary>
public static class PoreskaAmortizacijaCalculator
{
    public record RezultatPoreskeAmortizacije(
        int SredstvoId,
        int LegacySifra,
        string InventarskiBroj,
        string Naziv,
        DateTime DatumAktiviranja,
        string PoreskaGrupa,
        decimal PoreskaStopa,
        decimal PoreskaNabavnaVrednost,
        decimal PrethodnaPoreskaIspravka,
        decimal NovaPoreskaAmortizacija,
        decimal NovaPoreskaIspravka,
        decimal PoreskaNeotpisanaVrednost,
        decimal RacunovodstvenaAmortizacija,
        decimal PrivremenaPoreskaRazlika
    );

    /// <summary>
    /// Obračunava poresku amortizaciju za pojedinačno sredstvo (Obrazac OA).
    /// </summary>
    public static RezultatPoreskeAmortizacije IzracunajZaSredstvo(
        Sredstvo s,
        DateTime start,
        DateTime end,
        decimal racunovodstvenaAmortizacija)
    {
        decimal poreskaOsnovica = s.PoreskaNabavnaVrednost > 0 ? s.PoreskaNabavnaVrednost : s.NabavnaVrednost;
        decimal poreskaStopa = s.PoreskaStopa > 0 ? s.PoreskaStopa : s.StopaAmortizacije;
        string poreskaGrupa = !string.IsNullOrWhiteSpace(s.PoreskaGrupa) ? s.PoreskaGrupa : s.AmortizacionaGrupa;
        decimal prethodnaIspravka = s.PoreskaIspravkaVrednosti;

        decimal daniUGodini = DateTime.IsLeapYear(start.Year) ? 366m : 365m;

        DateTime calcStart = s.DatumAktiviranja > start ? s.DatumAktiviranja : start;
        decimal novaPoreskaAmortizacija = 0m;

        if (calcStart <= end && poreskaOsnovica > 0 && poreskaStopa > 0)
        {
            int days = (end - calcStart).Days + 1;
            if (days > 0)
            {
                novaPoreskaAmortizacija = (poreskaOsnovica * (poreskaStopa / 100m)) * days / daniUGodini;
            }
        }

        decimal neotpisana = Math.Max(0m, poreskaOsnovica - prethodnaIspravka);
        novaPoreskaAmortizacija = Math.Min(novaPoreskaAmortizacija, neotpisana);
        novaPoreskaAmortizacija = Math.Round(novaPoreskaAmortizacija, 2);

        decimal novaIspravka = Math.Round(prethodnaIspravka + novaPoreskaAmortizacija, 2);
        decimal novaNeotpisana = Math.Max(0m, Math.Round(poreskaOsnovica - novaIspravka, 2));

        decimal razlika = Math.Round(racunovodstvenaAmortizacija - novaPoreskaAmortizacija, 2);

        return new RezultatPoreskeAmortizacije(
            s.Id,
            s.LegacySifra,
            s.InventarskiBroj,
            s.Naziv,
            s.DatumAktiviranja,
            poreskaGrupa,
            poreskaStopa,
            poreskaOsnovica,
            prethodnaIspravka,
            novaPoreskaAmortizacija,
            novaIspravka,
            novaNeotpisana,
            racunovodstvenaAmortizacija,
            razlika
        );
    }

    public record SaldoGrupeResult(
        string Grupa,
        decimal Stopa,
        int BrojSredstava,
        decimal PocetniSaldo,
        decimal Nabavke,
        decimal Otudjenja,
        decimal OsnovicaZaAmortizaciju,
        decimal ObracunataAmortizacija,
        decimal KrajnjiSaldo,
        bool PrimijenjenMaliSaldo);

    /// <summary>
    /// Obračunava degresivni saldo grupa II-V za sredstva nabavljena pre 01.01.2019. (Član 4. i Član 7. Pravilnika).
    /// </summary>
    public static List<SaldoGrupeResult> IzracunajSaldoGrupaPre2019(
        IEnumerable<Sredstvo> sredstvaPre2019,
        decimal pragMaliSaldo = 675_000m)
    {
        var rezultati = new List<SaldoGrupeResult>();
        var grupe = new[]
        {
            new { Kod = "II", Stopa = 10.0m },
            new { Kod = "III", Stopa = 15.0m },
            new { Kod = "IV", Stopa = 20.0m },
            new { Kod = "V", Stopa = 30.0m }
        };

        foreach (var g in grupe)
        {
            var sredstvaGrupe = sredstvaPre2019
                .Where(s => string.Equals(s.PoreskaGrupa, g.Kod, StringComparison.OrdinalIgnoreCase) ||
                            (string.IsNullOrEmpty(s.PoreskaGrupa) && string.Equals(s.AmortizacionaGrupa, g.Kod, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            decimal pocetniSaldo = sredstvaGrupe.Sum(s => Math.Max(0m, (s.PoreskaNabavnaVrednost > 0 ? s.PoreskaNabavnaVrednost : s.NabavnaVrednost) - s.PoreskaIspravkaVrednosti));
            decimal nabavke = 0m;
            decimal otudjenja = 0m;
            decimal osnovica = Math.Max(0m, pocetniSaldo + nabavke - otudjenja);

            decimal obracunataAmortizacija = Math.Round(osnovica * (g.Stopa / 100m), 2);
            decimal krajnjiSaldo = Math.Max(0m, osnovica - obracunataAmortizacija);
            bool maliSaldo = false;

            // Član 7. Pravilnika: Ako je krajnji saldo grupe manji od 5 prosečnih bruto plata (pragMaliSaldo)
            if (krajnjiSaldo > 0 && krajnjiSaldo < pragMaliSaldo)
            {
                maliSaldo = true;
                obracunataAmortizacija = osnovica; // Celokupan saldo se priznaje kao rashod
                krajnjiSaldo = 0m;
            }

            rezultati.Add(new SaldoGrupeResult(
                g.Kod,
                g.Stopa,
                sredstvaGrupe.Count,
                pocetniSaldo,
                nabavke,
                otudjenja,
                osnovica,
                obracunataAmortizacija,
                krajnjiSaldo,
                maliSaldo
            ));
        }

        return rezultati;
    }
}
