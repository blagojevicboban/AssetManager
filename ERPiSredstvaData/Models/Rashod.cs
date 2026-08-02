namespace ERPiSredstvaData.Models;

/// <summary>
/// Tipovi promena na sredstvima — odgovara KOD polju u RASHOD.DBF
/// </summary>
public enum TipoviPromena
{
    Rashodovanje = 1,
    Prodaja = 2,
    Otudjenje = 3,
    KolicinskoRashodovanje = 4,
    PrenosUDrugOJ = 5,
    Brisanje = 6,
    PovecanjeVrednosti = 7,
    PovecanjeKolicine = 8,
    PovecanjeAmortizacije = 9
}

/// <summary>
/// Odgovara RASHOD.DBF — nalog za promenu/rashod osnovnog sredstva.
/// </summary>
public class Rashod
{
    public int Id { get; set; }
    public int BrojNaloga { get; set; }
    public int RedBroj { get; set; }

    public int SredstvoId { get; set; }
    public Sredstvo Sredstvo { get; set; } = null!;

    public TipoviPromena Kod { get; set; }
    public string KodTekst { get; set; } = string.Empty;
    public DateTime Datum { get; set; }
    public string DokumentBroj { get; set; } = string.Empty;
    public decimal Podaci { get; set; }
    public int ObracunskaJedinica { get; set; }
    public bool Knjizen { get; set; }
}
