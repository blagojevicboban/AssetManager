using SredstvaData.Models;

namespace SredstvaData.Services;

/// <summary>
/// Čista kalkulaciona logika za obračun revalorizacije, izdvojena iz RevalorizacijaPage
/// radi mogućnosti unit testiranja bez UI/DB zavisnosti.
/// </summary>
public static class RevalorizacijaCalculator
{
    public record Rezultat(decimal StaraNabavna, decimal StaraIspravka, decimal NovaNabavna, decimal NovaIspravka)
    {
        public decimal EfekatNabavna => NovaNabavna - StaraNabavna;
        public decimal EfekatIspravka => NovaIspravka - StaraIspravka;
        public bool ImaEfekat => Math.Abs(EfekatNabavna) >= 0.01m || Math.Abs(EfekatIspravka) >= 0.01m;
    }

    /// <summary>
    /// Obračunava efekat revalorizacije za jedno sredstvo u periodu [start, end].
    /// Promene opisane kao "Amortizacija" dobijaju godišnji koeficijent, ostale mesečni
    /// koeficijent za mesec u kom su nastale. <paramref name="mesecniKoefs"/> mora imati
    /// indekse 1-12 (mesec kao indeks).
    /// </summary>
    public static Rezultat Izracunaj(IEnumerable<Kartica> kartice, DateTime start, DateTime end, decimal godKoef, decimal[] mesecniKoefs)
    {
        var sorted = kartice.OrderBy(k => k.Datum).ToList();

        decimal staraNabavna = sorted.Where(k => k.Datum < start).Sum(k => k.NabavnaVrednost);
        decimal staraIspravka = sorted.Where(k => k.Datum < start).Sum(k => k.IspravkaVrednosti);

        decimal novaNabavna = staraNabavna * godKoef;
        decimal novaIspravka = staraIspravka * godKoef;

        var karticeUPeriodu = sorted.Where(k => k.Datum >= start && k.Datum <= end).ToList();

        foreach (var k in karticeUPeriodu)
        {
            staraNabavna += k.NabavnaVrednost;
            staraIspravka += k.IspravkaVrednosti;

            decimal primenjenKoef = k.OpisPromene.Contains("Amortizacija", StringComparison.OrdinalIgnoreCase)
                ? godKoef
                : mesecniKoefs[k.Datum.Month];

            novaNabavna += k.NabavnaVrednost * primenjenKoef;
            novaIspravka += k.IspravkaVrednosti * primenjenKoef;
        }

        return new Rezultat(staraNabavna, staraIspravka, novaNabavna, novaIspravka);
    }
}
