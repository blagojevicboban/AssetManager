using SredstvaData.Models;

namespace SredstvaApp;

public static class AppSession
{
    public static Korisnik? TrenutniKorisnik { get; set; }
    public static Firma? TrenutnaFirma { get; set; }
    
    public static bool IsAdmin => TrenutniKorisnik?.Uloga == UlogaKorisnika.Administrator;
    public static bool IsOperater => TrenutniKorisnik?.Uloga == UlogaKorisnika.Operater || IsAdmin;
    public static bool IsGledalac => TrenutniKorisnik?.Uloga == UlogaKorisnika.Gledalac || IsOperater;
}
