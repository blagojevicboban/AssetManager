using ERPiSredstvaData.Models;

namespace ERPiSredstvaData.Services;

/// <summary>
/// Čista logika generisanja popisnih stavki, izdvojena iz PopisPage
/// radi mogućnosti unit testiranja bez UI/DB zavisnosti.
/// </summary>
public static class PopisCalculator
{
    /// <summary>
    /// Generiše popisne stavke sa knjižnim stanjem preuzetim iz aktivnih sredstava
    /// (popisano stanje se podrazumeva jednako knjižnom dok se ne unese ručno).
    /// </summary>
    public static List<PopisnaStavka> GenerisiStavke(int popisId, IEnumerable<Sredstvo> aktivnaSredstva)
    {
        return aktivnaSredstva.Select(sredstvo =>
        {
            decimal knjiznaVrednost = sredstvo.NabavnaVrednost - sredstvo.IspravkaVrednosti;
            return new PopisnaStavka
            {
                PopisId = popisId,
                SredstvoId = sredstvo.Id,
                KnjiznaKolicina = sredstvo.Kolicina,
                KnjiznaVrednost = knjiznaVrednost,
                PopisanaKolicina = sredstvo.Kolicina,
                ProcenjenaVrednost = knjiznaVrednost
            };
        }).ToList();
    }
}
