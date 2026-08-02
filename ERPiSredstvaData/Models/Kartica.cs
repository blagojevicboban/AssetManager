namespace ERPiSredstvaData.Models;

/// <summary>
/// Odgovara KARTICA.DBF — hronološki log svih promena po sredstvu (audit trail).
/// </summary>
public class Kartica
{
    public int Id { get; set; }

    public int SredstvoId { get; set; }
    public Sredstvo Sredstvo { get; set; } = null!;

    public int RedBroj { get; set; }
    public DateTime Datum { get; set; }
    public string OpisPromene { get; set; } = string.Empty;
    public int ObracunskaJedinica { get; set; }
    public string Konto { get; set; } = string.Empty;
    public int AmortizacionaGrupa1 { get; set; }
    public int AmortizacionaGrupa2 { get; set; }
    public decimal StopaAmortizacije { get; set; }
    public decimal KoeficijentRevalorizacije { get; set; }
    public decimal Kolicina { get; set; }
    public decimal NabavnaVrednost { get; set; }
    public decimal IspravkaVrednosti { get; set; }
}
