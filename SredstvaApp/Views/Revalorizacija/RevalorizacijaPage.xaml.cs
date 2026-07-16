using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Revalorizacija;

public class RevalorizacijaResultViewModel
{
    public int SredstvoId { get; init; }
    public string InventarskiBroj { get; init; } = string.Empty;
    public string Naziv { get; init; } = string.Empty;
    public decimal PrimenjeniGodisnjiKoef { get; init; }
    
    public decimal StaraNabavna { get; init; }
    public decimal StaraIspravka { get; init; }
    
    public decimal NovaNabavna { get; init; }
    public decimal NovaIspravka { get; init; }
    
    public decimal EfekatNabavna => NovaNabavna - StaraNabavna;
    public decimal EfekatIspravka => NovaIspravka - StaraIspravka;
    public decimal NovaSadasnja => NovaNabavna - NovaIspravka;
    public string InventarskiBrojSort => System.Text.RegularExpressions.Regex.Replace(InventarskiBroj ?? "", @"\d+", m => m.Value.PadLeft(20, '0'));
}

public partial class RevalorizacijaPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<RevalorizacijaResultViewModel> _results = new();
    private DateTime _calcOd;
    private DateTime _calcDo;

    public RevalorizacijaPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += RevalorizacijaPage_Loaded;
    }

    private void RevalorizacijaPage_Loaded(object sender, RoutedEventArgs e)
    {
        var year = DateTime.Now.Year;
        DpOd.SelectedDate = new DateTime(year, 1, 1);
        DpDo.SelectedDate = new DateTime(year, 12, 31);
    }

    private decimal ParseKoef(TextBox tb)
    {
        if (decimal.TryParse(tb.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal val))
            return val;
        return 1m;
    }

    private void BtnObracunaj_Click(object sender, RoutedEventArgs e)
    {
        if (DpOd.SelectedDate == null || DpDo.SelectedDate == null)
        {
            MessageBox.Show("Molimo izaberite datume za period obračuna.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _calcOd = DpOd.SelectedDate.Value;
        _calcDo = DpDo.SelectedDate.Value;

        decimal godKoef = ParseKoef(TxtGodisnjiKoef);
        var mesecniKoefs = new decimal[13]; // Indeksi 1 do 12
        mesecniKoefs[1] = ParseKoef(TxtMesec1);
        mesecniKoefs[2] = ParseKoef(TxtMesec2);
        mesecniKoefs[3] = ParseKoef(TxtMesec3);
        mesecniKoefs[4] = ParseKoef(TxtMesec4);
        mesecniKoefs[5] = ParseKoef(TxtMesec5);
        mesecniKoefs[6] = ParseKoef(TxtMesec6);
        mesecniKoefs[7] = ParseKoef(TxtMesec7);
        mesecniKoefs[8] = ParseKoef(TxtMesec8);
        mesecniKoefs[9] = ParseKoef(TxtMesec9);
        mesecniKoefs[10] = ParseKoef(TxtMesec10);
        mesecniKoefs[11] = ParseKoef(TxtMesec11);
        mesecniKoefs[12] = ParseKoef(TxtMesec12);

        IzvrsiObracun(_calcOd, _calcDo, godKoef, mesecniKoefs);
    }

    private void IzvrsiObracun(DateTime start, DateTime end, decimal godKoef, decimal[] mesecniKoefs)
    {
        _results.Clear();

        var sredstva = _db.Sredstva
            .Include(s => s.Kartice)
            .Where(s => s.JeAktivno)
            .ToList();

        foreach (var s in sredstva)
        {
            var kartice = s.Kartice.OrderBy(k => k.Datum).ToList();
            
            // Baza pre perioda obračuna (do start datuma)
            decimal staraNabavna = kartice.Where(k => k.Datum < start).Sum(k => k.NabavnaVrednost);
            decimal staraIspravka = kartice.Where(k => k.Datum < start).Sum(k => k.IspravkaVrednosti);

            // Revalorizovana baza (primenjujemo godišnji koeficijent na nju)
            decimal novaNabavna = staraNabavna * godKoef;
            decimal novaIspravka = staraIspravka * godKoef;

            // Kartice unutar perioda obračuna
            var karticeUPeriodu = kartice.Where(k => k.Datum >= start && k.Datum <= end).ToList();

            foreach (var k in karticeUPeriodu)
            {
                staraNabavna += k.NabavnaVrednost;
                staraIspravka += k.IspravkaVrednosti;

                // Odabir koeficijenta za tekuću promenu
                decimal primenjenKoef = 1m;

                if (k.OpisPromene.Contains("Amortizacija", StringComparison.OrdinalIgnoreCase))
                {
                    // Redovna amortizacija iz ove godine dobija godišnji koeficijent
                    primenjenKoef = godKoef;
                }
                else
                {
                    // Ostale promene (npr. nabavka u toku godine) dobijaju mesečni koeficijent (za taj mesec)
                    primenjenKoef = mesecniKoefs[k.Datum.Month];
                }

                novaNabavna += k.NabavnaVrednost * primenjenKoef;
                novaIspravka += k.IspravkaVrednosti * primenjenKoef;
            }

            decimal efekatNabavna = novaNabavna - staraNabavna;
            decimal efekatIspravka = novaIspravka - staraIspravka;

            // Ukoliko nema efekta (koeficijenti su 1.00 i sl.), preskačemo sredstvo za prikaz
            if (Math.Abs(efekatNabavna) < 0.01m && Math.Abs(efekatIspravka) < 0.01m)
                continue;

            _results.Add(new RevalorizacijaResultViewModel
            {
                SredstvoId = s.Id,
                InventarskiBroj = s.InventarskiBroj,
                Naziv = s.Naziv,
                PrimenjeniGodisnjiKoef = godKoef,
                StaraNabavna = staraNabavna,
                StaraIspravka = staraIspravka,
                NovaNabavna = novaNabavna,
                NovaIspravka = novaIspravka
            });
        }

        _results = _results.OrderBy(r => r.InventarskiBroj).ToList();

        RevalorizacijaGrid.ItemsSource = _results;
        
        PlaceholderPanel.Visibility = Visibility.Collapsed;
        RevalorizacijaGrid.Visibility = Visibility.Visible;

        decimal ukupnoEfekatNabavna = _results.Sum(r => r.EfekatNabavna);
        decimal ukupnoEfekatIspravka = _results.Sum(r => r.EfekatIspravka);

        UkupnoEfekatNabavnaTxt.Text = ukupnoEfekatNabavna.ToString("N2");
        UkupnoEfekatIspravkaTxt.Text = ukupnoEfekatIspravka.ToString("N2");

        BtnExport.IsEnabled = true;
        BtnStampa.IsEnabled = true;
        BtnProknjizi.IsEnabled = _results.Any();
    }

    private void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        var msg = MessageBox.Show(
            $"Da li ste sigurni da želite da proknjižite revalorizaciju?\n\n" +
            "Ova akcija će kreirati stavke u karticama i trajno uskladiti vrednosti sredstava.",
            "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (msg != MessageBoxResult.Yes) return;

        int proknjizeno = 0;
        using var transaction = _db.Database.BeginTransaction();

        try
        {
            foreach (var res in _results)
            {
                var sredstvo = _db.Sredstva.Find(res.SredstvoId);
                if (sredstvo == null) continue;

                // Nova kartica za revalorizaciju
                var kartica = new Kartica
                {
                    SredstvoId = res.SredstvoId,
                    Datum = _calcDo,
                    OpisPromene = $"Revalorizacija ({_calcOd.Year})",
                    ObracunskaJedinica = 1,
                    Konto = "", 
                    AmortizacionaGrupa1 = 0,
                    AmortizacionaGrupa2 = 0,
                    StopaAmortizacije = sredstvo.StopaAmortizacije,
                    KoeficijentRevalorizacije = res.PrimenjeniGodisnjiKoef,
                    Kolicina = 0,
                    NabavnaVrednost = res.EfekatNabavna,
                    IspravkaVrednosti = res.EfekatIspravka
                };

                // Preuzimanje organizacionih podataka iz poslednje kartice
                var lastKartica = _db.Kartice.Where(k => k.SredstvoId == res.SredstvoId).OrderByDescending(k => k.Datum).FirstOrDefault();
                if (lastKartica != null)
                {
                    kartica.Konto = lastKartica.Konto;
                    kartica.ObracunskaJedinica = lastKartica.ObracunskaJedinica;
                    kartica.AmortizacionaGrupa1 = lastKartica.AmortizacionaGrupa1;
                    kartica.AmortizacionaGrupa2 = lastKartica.AmortizacionaGrupa2;
                }

                var maxRbr = _db.Kartice.Where(k => k.SredstvoId == res.SredstvoId).Max(k => (int?)k.RedBroj) ?? 0;
                kartica.RedBroj = maxRbr + 1;

                _db.Kartice.Add(kartica);

                // Ažuriranje Sredstva
                sredstvo.NabavnaVrednost += res.EfekatNabavna;
                sredstvo.IspravkaVrednosti += res.EfekatIspravka;
                sredstvo.SadasnjaVrednost = sredstvo.NabavnaVrednost - sredstvo.IspravkaVrednosti;

                proknjizeno++;
            }

            _db.SaveChanges();
            transaction.Commit();

            MessageBox.Show($"Uspešno je proknjižena revalorizacija za {proknjizeno} sredstava.", 
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reset UI
            _results.Clear();
            RevalorizacijaGrid.ItemsSource = null;
            RevalorizacijaGrid.Visibility = Visibility.Collapsed;
            PlaceholderPanel.Visibility = Visibility.Visible;
            BtnProknjizi.IsEnabled = false;
            BtnExport.IsEnabled = false;
            BtnStampa.IsEnabled = false;
            UkupnoEfekatNabavnaTxt.Text = "0.00";
            UkupnoEfekatIspravkaTxt.Text = "0.00";
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            MessageBox.Show($"Greška pri knjiženju: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        if (_results.Count == 0)
        {
            MessageBox.Show("Nema podataka za štampu. Pokrenite obračun.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var nazivFirme = _db.Firme.FirstOrDefault()?.Naziv ?? "Nepoznata firma";
            var godKoef = ParseKoef(TxtGodisnjiKoef); // Učitavanje sa polja jer nam treba za zaglavlje PDF-a
            var doc = new RevalorizacijaDocument(_results, nazivFirme, _calcOd, _calcDo, godKoef);
            var tempFile = Path.Combine(Path.GetTempPath(), $"Revalorizacija_{_calcOd.Year}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            doc.GeneratePdf(tempFile);
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Sačuvaj izveštaj revalorizacije",
            Filter = "CSV fajl (*.csv)|*.csv",
            FileName = $"revalorizacija_{_calcOd.Year}"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Inv. Broj;Naziv Sredstva;Stara Nabavna;Stara Ispravka;Efekat Nabavna;Efekat Ispravka;Nova Sadasnja");

            foreach (var r in _results)
            {
                sb.AppendLine($"{r.InventarskiBroj};{r.Naziv};{r.StaraNabavna:F2};{r.StaraIspravka:F2};{r.EfekatNabavna:F2};{r.EfekatIspravka:F2};{r.NovaSadasnja:F2}");
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Izveštaj sačuvan:\n{dlg.FileName}", "Export uspešan", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri eksportu: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
