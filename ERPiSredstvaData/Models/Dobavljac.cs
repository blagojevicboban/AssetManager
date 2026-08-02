namespace ERPiSredstvaData.Models;

public class Dobavljac
{
    public int Id { get; set; }
    public int Konto { get; set; }
    public string OpisKonta { get; set; } = string.Empty;
    public string UlicaIBroj { get; set; } = string.Empty;
    public string MestoIBroj { get; set; } = string.Empty;

    public ICollection<Prijava> Prijave { get; set; } = new List<Prijava>();
}
