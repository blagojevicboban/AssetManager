using ERPiSredstvaData.Models;
using ERPiSredstvaData.Services;

namespace ERPiSredstvaData.Tests;

public class PopisCalculatorTests
{
    [Fact]
    public void GenerisiStavke_PreuzimaKnjiznoStanjeIzSredstva()
    {
        var sredstva = new List<Sredstvo>
        {
            new() { Id = 1, Kolicina = 3m, NabavnaVrednost = 10_000m, IspravkaVrednosti = 4_000m }
        };

        var stavke = PopisCalculator.GenerisiStavke(popisId: 7, sredstva);

        var stavka = Assert.Single(stavke);
        Assert.Equal(7, stavka.PopisId);
        Assert.Equal(1, stavka.SredstvoId);
        Assert.Equal(3m, stavka.KnjiznaKolicina);
        Assert.Equal(6_000m, stavka.KnjiznaVrednost);
    }

    [Fact]
    public void GenerisiStavke_PopisanoStanjePodrazumevanoJednakoKnjiznom()
    {
        var sredstva = new List<Sredstvo>
        {
            new() { Id = 1, Kolicina = 2m, NabavnaVrednost = 5_000m, IspravkaVrednosti = 1_000m }
        };

        var stavka = Assert.Single(PopisCalculator.GenerisiStavke(1, sredstva));

        Assert.Equal(stavka.KnjiznaKolicina, stavka.PopisanaKolicina);
        Assert.Equal(stavka.KnjiznaVrednost, stavka.ProcenjenaVrednost);
        Assert.Equal(0m, stavka.Razlika);
        Assert.False(stavka.ImaRazliku);
    }

    [Fact]
    public void GenerisiStavke_PraznaListaSredstava_DajePraznuListuStavki()
    {
        var stavke = PopisCalculator.GenerisiStavke(1, Enumerable.Empty<Sredstvo>());

        Assert.Empty(stavke);
    }
}
