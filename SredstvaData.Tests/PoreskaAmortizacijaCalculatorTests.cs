using System;
using System.Collections.Generic;
using SredstvaData.Models;
using SredstvaData.Services;
using Xunit;

namespace SredstvaData.Tests;

public class PoreskaAmortizacijaCalculatorTests
{
    [Fact]
    public void PoreskaAmortizacija_ObracunavaPoreskuStopuIOsnovicu()
    {
        var sredstvo = new Sredstvo
        {
            Id = 1,
            InventarskiBroj = "INV-001",
            Naziv = "Test Oprema",
            DatumAktiviranja = new DateTime(2020, 1, 1),
            NabavnaVrednost = 100_000m,
            StopaAmortizacije = 20m,
            PoreskaNabavnaVrednost = 100_000m,
            PoreskaStopa = 15m,
            PoreskaIspravkaVrednosti = 0m,
            PoreskaGrupa = "III"
        };

        var res = PoreskaAmortizacijaCalculator.IzracunajZaSredstvo(
            sredstvo,
            start: new DateTime(2026, 1, 1),
            end: new DateTime(2026, 12, 31),
            racunovodstvenaAmortizacija: 20_000m);

        // 15% od 100,000 = 15,000 Poreska amortizacija
        Assert.Equal(15_000m, res.NovaPoreskaAmortizacija);
        Assert.Equal(85_000m, res.PoreskaNeotpisanaVrednost);
        // Privremena razlika = Racunovodstvena (20,000) - Poreska (15,000) = 5,000
        Assert.Equal(5_000m, res.PrivremenaPoreskaRazlika);
    }

    [Fact]
    public void PoreskaAmortizacija_NeMozePreciNeotpisanuPoreskuVrednost()
    {
        var sredstvo = new Sredstvo
        {
            Id = 2,
            InventarskiBroj = "INV-002",
            Naziv = "Skoro Otpisana Oprema",
            DatumAktiviranja = new DateTime(2020, 1, 1),
            NabavnaVrednost = 100_000m,
            PoreskaNabavnaVrednost = 100_000m,
            PoreskaStopa = 20m,
            PoreskaIspravkaVrednosti = 98_000m
        };

        var res = PoreskaAmortizacijaCalculator.IzracunajZaSredstvo(
            sredstvo,
            start: new DateTime(2026, 1, 1),
            end: new DateTime(2026, 12, 31),
            racunovodstvenaAmortizacija: 2_000m);

        Assert.Equal(2_000m, res.NovaPoreskaAmortizacija);
        Assert.Equal(0m, res.PoreskaNeotpisanaVrednost);
    }
}
