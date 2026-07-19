using SredstvaData.Models;
using SredstvaData.Services;

namespace SredstvaData.Tests;

public class RevalorizacijaCalculatorTests
{
    private static Kartica K(DateTime datum, string opis = "Nabavka", decimal nabavna = 0, decimal ispravka = 0) => new()
    {
        Datum = datum,
        OpisPromene = opis,
        NabavnaVrednost = nabavna,
        IspravkaVrednosti = ispravka
    };

    private static decimal[] Mesecni(decimal svakiMesec = 1m)
    {
        var arr = new decimal[13];
        for (int i = 1; i <= 12; i++) arr[i] = svakiMesec;
        return arr;
    }

    [Fact]
    public void BazaPrePerioda_MnoziSeGodisnjimKoeficijentom()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m, ispravka: 30_000m) };

        var rezultat = RevalorizacijaCalculator.Izracunaj(kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), godKoef: 1.10m, Mesecni());

        Assert.Equal(110_000m, rezultat.NovaNabavna);
        Assert.Equal(33_000m, rezultat.NovaIspravka);
        Assert.Equal(10_000m, rezultat.EfekatNabavna);
        Assert.Equal(3_000m, rezultat.EfekatIspravka);
    }

    [Fact]
    public void PromenaOznacenaKaoAmortizacija_DobijaGodisnjiKoeficijent()
    {
        var kartice = new List<Kartica>
        {
            K(new DateTime(2026, 3, 15), opis: "Amortizacija (2025)", ispravka: 1_000m)
        };
        var mesecni = Mesecni(2m); // mesecni koef razlicit od godisnjeg da se razlika vidi

        var rezultat = RevalorizacijaCalculator.Izracunaj(kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), godKoef: 1.05m, mesecni);

        // Ocekujemo da je primenjen godisnji koeficijent (1.05), ne mesecni (2.0)
        Assert.Equal(1_000m * 1.05m, rezultat.NovaIspravka);
    }

    [Fact]
    public void OstalePromene_DobijajuMesecniKoeficijentZaMesecPromene()
    {
        var mesecni = Mesecni();
        mesecni[3] = 1.5m; // mart

        var kartice = new List<Kartica>
        {
            K(new DateTime(2026, 3, 10), opis: "Nabavka", nabavna: 10_000m)
        };

        var rezultat = RevalorizacijaCalculator.Izracunaj(kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), godKoef: 1.0m, mesecni);

        Assert.Equal(10_000m * 1.5m, rezultat.NovaNabavna - 0m);
    }

    [Fact]
    public void ImaEfekat_FalseKadaSuKoeficijentiNeutralni()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m, ispravka: 20_000m) };

        var rezultat = RevalorizacijaCalculator.Izracunaj(kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), godKoef: 1.0m, Mesecni());

        Assert.False(rezultat.ImaEfekat);
    }

    [Fact]
    public void ImaEfekat_TrueKadaEfekatPrelaziPrag()
    {
        var kartice = new List<Kartica> { K(new DateTime(2020, 1, 1), nabavna: 100_000m) };

        var rezultat = RevalorizacijaCalculator.Izracunaj(kartice, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), godKoef: 1.001m, Mesecni());

        Assert.True(rezultat.ImaEfekat);
    }
}
