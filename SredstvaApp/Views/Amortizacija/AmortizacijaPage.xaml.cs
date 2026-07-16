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

namespace SredstvaApp.Views.Amortizacija;

public class AmortizacijaResultViewModel
{
    public int SredstvoId { get; init; }
    public string InventarskiBroj { get; init; } = string.Empty;
    public string Naziv { get; init; } = string.Empty;
    public int ObracunskaJedinica { get; init; }
    public string Konto { get; init; } = string.Empty;
    public string AmortizacionaGrupa { get; init; } = string.Empty;
    public decimal StopaAmortizacije { get; init; }
    public decimal NabavnaVrednost { get; init; }
    public decimal PrethodnaIspravka { get; init; }
    public decimal NovaAmortizacija { get; init; }
    
    public decimal NovaIspravkaUkupno => PrethodnaIspravka + NovaAmortizacija;
    public decimal SadasnjaVrednost => NabavnaVrednost - NovaIspravkaUkupno;
    public string InventarskiBrojSort => System.Text.RegularExpressions.Regex.Replace(InventarskiBroj ?? "", @"\d+", m => m.Value.PadLeft(20, '0'));
}

public partial class AmortizacijaPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<AmortizacijaResultViewModel> _results = new();
    private DateTime _calcOd;
    private DateTime _calcDo;

    public AmortizacijaPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += AmortizacijaPage_Loaded;
    }

    private void AmortizacijaPage_Loaded(object sender, RoutedEventArgs e)
    {
        var year = DateTime.Now.Year;
        DpOd.SelectedDate = new DateTime(year, 1, 1);
        DpDo.SelectedDate = new DateTime(year, 12, 31);
    }

    private void BtnObracunaj_Click(object sender, RoutedEventArgs e)
    {
        if (DpOd.SelectedDate == null || DpDo.SelectedDate == null)
        {
            MessageBox.Show("Molimo izaberite datume za period obračuna.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DpOd.SelectedDate > DpDo.SelectedDate)
        {
            MessageBox.Show("Datum 'Od' mora biti pre datuma 'Do'.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _calcOd = DpOd.SelectedDate.Value;
        _calcDo = DpDo.SelectedDate.Value;

        IzvrsiObracun(_calcOd, _calcDo);
    }

    private void IzvrsiObracun(DateTime start, DateTime end)
    {
        _results.Clear();

        // Učitavamo sva aktivna sredstva sa njihovim karticama (koje prethode ili su unutar perioda)
        var sredstva = _db.Sredstva
            .Include(s => s.Kartice)
            .Where(s => s.JeAktivno)
            .ToList();

        foreach (var s in sredstva)
        {
            // Kartice sortirane hronološki
            var sveKartice = s.Kartice.OrderBy(k => k.Datum).ToList();
            
            // Stanje pre perioda obračuna
            decimal tekucaNabavna = sveKartice.Where(k => k.Datum < start).Sum(k => k.NabavnaVrednost);
            decimal tekucaIspravka = sveKartice.Where(k => k.Datum < start).Sum(k => k.IspravkaVrednosti);

            decimal ukupnaNovaAmortizacija = 0;
            DateTime currentDate = start;
            decimal daniUGodini = DateTime.IsLeapYear(start.Year) ? 366m : 365m;

            // Kartice unutar perioda obračuna
            var karticeUPeriodu = sveKartice.Where(k => k.Datum >= start && k.Datum <= end).ToList();

            foreach (var kartica in karticeUPeriodu)
            {
                int days = (kartica.Datum - currentDate).Days;
                if (days > 0 && tekucaNabavna > 0)
                {
                    ukupnaNovaAmortizacija += (tekucaNabavna * (s.StopaAmortizacije / 100m)) * days / daniUGodini;
                }

                // Ažuriramo tekuće stanje (npr. nabavka, rashod, revalorizacija unutar godine)
                tekucaNabavna += kartica.NabavnaVrednost;
                tekucaIspravka += kartica.IspravkaVrednosti;
                currentDate = kartica.Datum;
            }

            // Ostatak perioda (do kraja obračuna)
            int finalDays = (end - currentDate).Days + 1; // +1 da bi se obuhvatio i sam krajnji datum
            if (finalDays > 0 && tekucaNabavna > 0)
            {
                ukupnaNovaAmortizacija += (tekucaNabavna * (s.StopaAmortizacije / 100m)) * finalDays / daniUGodini;
            }

            // Ograničenje: Nova ispravka ne sme da pređe neotpisanu vrednost (nabavna - dosadašnja ispravka)
            decimal neotpisanaVrednost = tekucaNabavna - tekucaIspravka;
            if (neotpisanaVrednost < 0) neotpisanaVrednost = 0;

            ukupnaNovaAmortizacija = Math.Min(ukupnaNovaAmortizacija, neotpisanaVrednost);

            // Dodajemo u rezultate samo ako ima promena (ili ako zelimo sve aktivne)
            // Stari sistem generiše za sva aktivna sredstva, pa ćemo prikazati sva
            _results.Add(new AmortizacijaResultViewModel
            {
                SredstvoId = s.Id,
                InventarskiBroj = s.InventarskiBroj,
                Naziv = s.Naziv,
                ObracunskaJedinica = s.ObracunskaJedinica,
                Konto = s.Konto,
                AmortizacionaGrupa = s.AmortizacionaGrupa,
                StopaAmortizacije = s.StopaAmortizacije,
                NabavnaVrednost = tekucaNabavna,
                PrethodnaIspravka = tekucaIspravka,
                NovaAmortizacija = Math.Round(ukupnaNovaAmortizacija, 2)
            });
        }

        // Sortiramo po inventarskom broju (ili amortizacionoj grupi)
        _results = _results.OrderBy(r => r.InventarskiBroj).ToList();

        // Prikaz rezultata
        AmortizacijaGrid.ItemsSource = _results;
        
        PlaceholderPanel.Visibility = Visibility.Collapsed;
        AmortizacijaGrid.Visibility = Visibility.Visible;

        decimal ukupnoNova = _results.Sum(r => r.NovaAmortizacija);
        UkupnaNovaAmortizacijaTxt.Text = ukupnoNova.ToString("N2");
        BrojStavkiTxt.Text = $"(Za {_results.Count} sredstava)";

        BtnExport.IsEnabled = true;
        BtnStampa.IsEnabled = true;
        BtnProknjizi.IsEnabled = ukupnoNova > 0;
    }

    private void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        var msg = MessageBox.Show(
            $"Da li ste sigurni da želite da proknjižite obračun za period {_calcOd:dd.MM.yyyy} - {_calcDo:dd.MM.yyyy}?\n\n" +
            "Ova akcija će kreirati stavke u karticama i ažurirati vrednosti sredstava.",
            "Potvrda knjiženja", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (msg != MessageBoxResult.Yes) return;

        int proknjizeno = 0;
        using var transaction = _db.Database.BeginTransaction();

        try
        {
            foreach (var res in _results.Where(r => r.NovaAmortizacija > 0))
            {
                var sredstvo = _db.Sredstva.Find(res.SredstvoId);
                if (sredstvo == null) continue;

                // 1. Nova kartica za amortizaciju
                var kartica = new Kartica
                {
                    SredstvoId = res.SredstvoId,
                    Datum = _calcDo,
                    OpisPromene = $"Amortizacija ({_calcOd.Year})",
                    ObracunskaJedinica = 1, // Ili neka specifična OJ
                    Konto = "", // Ako treba specifičan konto, preuzeti sa sredstva ili prethodne kartice
                    AmortizacionaGrupa1 = 0,
                    AmortizacionaGrupa2 = 0,
                    StopaAmortizacije = sredstvo.StopaAmortizacije,
                    KoeficijentRevalorizacije = 0,
                    Kolicina = 0,
                    NabavnaVrednost = 0,
                    IspravkaVrednosti = res.NovaAmortizacija
                };

                // Pokušaj da preuzmeš Konto/OJ iz poslednje kartice
                var lastKartica = _db.Kartice.Where(k => k.SredstvoId == res.SredstvoId).OrderByDescending(k => k.Datum).FirstOrDefault();
                if (lastKartica != null)
                {
                    kartica.Konto = lastKartica.Konto;
                    kartica.ObracunskaJedinica = lastKartica.ObracunskaJedinica;
                    kartica.AmortizacionaGrupa1 = lastKartica.AmortizacionaGrupa1;
                    kartica.AmortizacionaGrupa2 = lastKartica.AmortizacionaGrupa2;
                }

                // Generisanje RedBroj
                var maxRbr = _db.Kartice.Where(k => k.SredstvoId == res.SredstvoId).Max(k => (int?)k.RedBroj) ?? 0;
                kartica.RedBroj = maxRbr + 1;

                _db.Kartice.Add(kartica);

                // 2. Ažuriranje samog Sredstva
                sredstvo.IspravkaVrednosti += res.NovaAmortizacija;
                sredstvo.SadasnjaVrednost = sredstvo.NabavnaVrednost - sredstvo.IspravkaVrednosti;

                proknjizeno++;
            }

            _db.SaveChanges();
            transaction.Commit();

            MessageBox.Show($"Uspešno je proknjižena amortizacija za {proknjizeno} sredstava.", 
                "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);

            // Očistimo UI jer je knjiženje završeno
            _results.Clear();
            AmortizacijaGrid.ItemsSource = null;
            AmortizacijaGrid.Visibility = Visibility.Collapsed;
            PlaceholderPanel.Visibility = Visibility.Visible;
            BtnProknjizi.IsEnabled = false;
            BtnExport.IsEnabled = false;
            UkupnaNovaAmortizacijaTxt.Text = "0.00";
            BrojStavkiTxt.Text = "";
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
            var firma = _db.Firme.FirstOrDefault();
            var doc = new AmortizacijaDocument(_results, firma, _calcOd, _calcDo);
            var tempFile = Path.Combine(Path.GetTempPath(), $"Amortizacija_{_calcOd.Year}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
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
            Title = "Sačuvaj izveštaj amortizacije",
            Filter = "CSV fajl (*.csv)|*.csv",
            FileName = $"amortizacija_{_calcOd.Year}"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("Inv. Broj;Naziv Sredstva;Stopa %;Nabavna Vrednost;Prethodna Ispravka;Nova Amortizacija;Sadasnja Vrednost");

            foreach (var r in _results)
            {
                sb.AppendLine($"{r.InventarskiBroj};{r.Naziv};{r.StopaAmortizacije:F2};{r.NabavnaVrednost:F2};{r.PrethodnaIspravka:F2};{r.NovaAmortizacija:F2};{r.SadasnjaVrednost:F2}");
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
