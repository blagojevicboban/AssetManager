namespace ERPiSredstvaData.Models;

public class ClanKomisije
{
    public int Id { get; set; }
    
    public int KomisijaId { get; set; }
    public Komisija Komisija { get; set; } = null!;
    
    public string ImePrezime { get; set; } = string.Empty;
    public string Uloga { get; set; } = "Član"; // npr. Predsednik, Član
}
