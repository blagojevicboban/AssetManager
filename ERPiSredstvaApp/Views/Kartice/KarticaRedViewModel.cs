using ERPiSredstvaData.Models;

namespace ERPiSredstvaApp.Views.Kartice;

/// <summary>
/// Wrapper oko Kartica entiteta koji dodaje izvedena polja za prikaz.
/// </summary>
public class KarticaRedViewModel
{
    private readonly Kartica _k;

    public KarticaRedViewModel(Kartica k, decimal kumulativnaSadasnja)
    {
        _k = k;
        SadasnjaVrednostKumulativna = kumulativnaSadasnja;
    }

    public int RedBroj => _k.RedBroj;
    public DateTime Datum => _k.Datum;
    public string OpisPromene => _k.OpisPromene;
    public int ObracunskaJedinica => _k.ObracunskaJedinica;
    public string Konto => _k.Konto;
    public string AmGrupaFormatted => $"{_k.AmortizacionaGrupa1}/{_k.AmortizacionaGrupa2}";
    public decimal StopaAmortizacije => _k.StopaAmortizacije;
    public decimal KoeficijentRevalorizacije => _k.KoeficijentRevalorizacije;
    public decimal NabavnaVrednost => _k.NabavnaVrednost;
    public decimal IspravkaVrednosti => _k.IspravkaVrednosti;

    /// <summary>Kumulativna sadašnja vrednost (nabavna - ispravka) do ovog reda.</summary>
    public decimal SadasnjaVrednostKumulativna { get; }
}
