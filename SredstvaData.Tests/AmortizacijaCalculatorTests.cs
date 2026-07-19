using SredstvaData.Models;
using SredstvaData.Services;

namespace SredstvaData.Tests;

public class AmortizacijaCalculatorTests
{
    private static Kartica K(DateTime datum, decimal nabavna = 0, decimal ispravka = 0, int id = 0) => new()
    {
        Id = id,
        Datum = datum,
        NabavnaVrednost = nabavna,
        IspravkaVrednosti = ispravka
    };

    [Fact]
    public void PunaGodina_BezPromena_ObracunavaCelogodisnjuStopu()
    {
        // Sredstvo nabavljeno pre perioda, nema kartica u periodu obracuna.
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(20m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(100_000m, rezultat.NabavnaVrednost);
        Assert.Equal(0m, rezultat.PrethodnaIspravka);
        // 20% od 100000 za celu (neprestupnu) godinu = 20000
        Assert.Equal(20_000m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void PrestupnaGodina_Koristi366Dana()
    {
        var kartice = new List<Kartica> { K(new DateTime(2019, 1, 1), nabavna: 100_000m) };

        // 2024 je prestupna godina
        var rezultat = AmortizacijaCalculator.Izracunaj(36.6m, kartice, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // 36.6% * 100000 * 366/366 = 36600 (da je 365 dana rezultat bi bio malo drugaciji zbog zaokruzivanja)
        Assert.Equal(36_600m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void NabavkaUsredGodine_ObracunavaSamoOdDatumaNabavke()
    {
        // Sredstvo nabavljeno 1.7. (pola godine pre kraja obracuna 31.12.)
        var kartice = new List<Kartica> { K(new DateTime(2026, 7, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(20m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        // Dani od 1.7. do 31.12. uklj. = 184 (2026 nije prestupna, 365 dana u godini)
        int ocekivaniDani = (new DateTime(2026, 12, 31) - new DateTime(2026, 7, 1)).Days + 1;
        decimal ocekivano = Math.Round(100_000m * 0.20m * ocekivaniDani / 365m, 2);

        Assert.Equal(ocekivano, rezultat.NovaAmortizacija);
        Assert.Equal(100_000m, rezultat.NabavnaVrednost);
    }

    [Fact]
    public void NovaAmortizacija_OgranicenaNaNeotpisanuVrednost()
    {
        // Sredstvo skoro potpuno otpisano - nova amortizacija ne sme preci preostalu neotpisanu vrednost.
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m, ispravka: 99_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(50m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(1_000m, rezultat.NovaAmortizacija);
    }

    [Fact]
    public void RashodUsredPerioda_SmanjujeOsnovicuZaNarednoObracunavanje()
    {
        // Nabavka pre perioda, pa delimicni rashod (negativna nabavna vrednost) sredinom godine.
        var kartice = new List<Kartica>
        {
            K(new DateTime(2020, 1, 1), nabavna: 100_000m),
            K(new DateTime(2026, 7, 2), nabavna: -40_000m)
        };

        var rezultat = AmortizacijaCalculator.Izracunaj(20m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        // Nakon rashoda 2.7. preostaje 60000 osnovice za drugi deo godine.
        Assert.Equal(60_000m, rezultat.NabavnaVrednost);
    }

    [Fact]
    public void NultaStopaAmortizacije_DajeNuluBezObziraNaOsnovicu()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m) };

        var rezultat = AmortizacijaCalculator.Izracunaj(0m, kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        Assert.Equal(0m, rezultat.NovaAmortizacija);
    }

    [Theory]
    [InlineData("Amortizacija (2026)", true, 2026)]
    [InlineData("Redovan otpis (2025)", true, 2025)]
    [InlineData("Redovan otpis 2024", true, 2024)]
    [InlineData("Revalorizacija (2026)", false, 0)]
    [InlineData("Nabavka", false, 0)]
    public void TryParseGodina_PrepoznajeGodinuIzOpisaPromene(string opis, bool ocekivanUspeh, int ocekivanaGodina)
    {
        bool uspeh = AmortizacijaCalculator.TryParseGodina(opis, out int godina);

        Assert.Equal(ocekivanUspeh, uspeh);
        if (ocekivanUspeh)
            Assert.Equal(ocekivanaGodina, godina);
    }

    [Fact]
    public void IzracunajPrethodnuIspravku_SabiraSamoKarticePreDatogTrenutka()
    {
        var pre1 = K(new DateTime(2025, 1, 1), ispravka: 100m, id: 1);
        var pre2 = K(new DateTime(2025, 6, 1), ispravka: 50m, id: 2);
        var trenutna = K(new DateTime(2026, 1, 1), ispravka: 200m, id: 3);
        var posle = K(new DateTime(2026, 6, 1), ispravka: 999m, id: 4);

        var sve = new List<Kartica> { pre1, pre2, trenutna, posle };

        decimal prethodna = AmortizacijaCalculator.IzracunajPrethodnuIspravku(sve, trenutna);

        Assert.Equal(150m, prethodna);
    }

    [Fact]
    public void IzracunajPrethodnuIspravku_IstiDatum_KoristiIdKaoTieBreaker()
    {
        var datum = new DateTime(2026, 1, 1);
        var ranija = K(datum, ispravka: 10m, id: 1);
        var trenutna = K(datum, ispravka: 20m, id: 2);
        var kasnija = K(datum, ispravka: 30m, id: 3);

        var sve = new List<Kartica> { ranija, trenutna, kasnija };

        decimal prethodna = AmortizacijaCalculator.IzracunajPrethodnuIspravku(sve, trenutna);

        Assert.Equal(10m, prethodna);
    }
}
