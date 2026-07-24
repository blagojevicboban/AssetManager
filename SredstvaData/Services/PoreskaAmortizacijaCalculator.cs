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

        decimal novaIspravka = prethodnaIspravka + novaPoreskaAmortizacija;
        decimal preostalaNeotpisana = Math.Max(0m, poreskaOsnovica - novaIspravka);
        decimal privremenaRazlika = racunovodstvenaAmortizacija - novaPoreskaAmortizacija;

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
            preostalaNeotpisana,
            racunovodstvenaAmortizacija,
            privremenaRazlika
        );
    }
}
