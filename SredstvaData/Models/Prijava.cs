namespace SredstvaData.Models;

/// <summary>
/// Odgovara PRIJAVA.DBF — nalog za aktiviranje/prijem osnovnog sredstva.
/// </summary>
public class Prijava
{
    public int Id { get; set; }
    public int BrojNaloga { get; set; }
    public int RedBroj { get; set; }

    public int SredstvoId { get; set; }
    public Sredstvo Sredstvo { get; set; } = null!;

    public int ObracunskaJedinica { get; set; }
    public string Konto { get; set; } = string.Empty;
    public int AmortizacionaGrupa1 { get; set; }
    public int AmortizacionaGrupa2 { get; set; }
    public decimal StopaAmortizacije { get; set; }
    public DateTime DatumAktiviranja { get; set; }
    public int RevalorizacionaGrupa { get; set; }
    public decimal NabavnaVrednost { get; set; }
    public decimal OtpisanaVrednost { get; set; }
    public string JedinicaMere { get; set; } = string.Empty;
    public decimal Kolicina { get; set; }
    public string InventarskiBroj { get; set; } = string.Empty;
    public string BrojFakture { get; set; } = string.Empty;
    public DateTime? DatumFakture { get; set; }
    public int BrojNalaznice { get; set; }
    public string BrNal { get; set; } = string.Empty;
    public int GodNal { get; set; }
    public bool Knjizen { get; set; }

    public int? DobavljacId { get; set; }
    public Dobavljac? Dobavljac { get; set; }
}
