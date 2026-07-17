using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.IO;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SredstvaData;
using SredstvaData.Models;
using SredstvaApp.Views.Rashod.Stampe;

namespace SredstvaApp.Views.Rashod;

public class PrijavaStavkaViewModel
{
    public int RedBroj { get; set; }
    public string InventarskiBroj { get; set; } = string.Empty;
    public string Naziv { get; set; } = string.Empty;
    public decimal NabavnaVrednost { get; set; }
    public decimal StopaAmortizacije { get; set; }
    public string AmortizacionaGrupa { get; set; } = string.Empty;
    public string Konto { get; set; } = string.Empty;
    public int ObracunskaJedinica { get; set; }
    public decimal Kolicina { get; set; } = 1m;
    public decimal OtpisanaVrednost { get; set; }
    public int Sifra { get; set; }
    public string BrojFakture { get; set; } = string.Empty;
}

public partial class PrijavaWindow : Window
{
    private readonly SredstvaDbContext _db;
    private readonly int? _brojNaloga;
    public ObservableCollection<PrijavaStavkaViewModel> Stavke { get; set; } = new();

    public PrijavaWindow(SredstvaDbContext db, int? brojNaloga = null)
    {
        InitializeComponent();
        _db = db;
        _brojNaloga = brojNaloga;
        DataContext = this;
        Loaded += PrijavaWindow_Loaded;
    }

    private void PrijavaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        CmbDobavljac.ItemsSource = _db.Dobavljaci.OrderBy(d => d.OpisKonta).ToList();
        DpDatum.SelectedDate = DateTime.Today;

        if (_brojNaloga.HasValue)
        {
            Title = $"Pregled Prijave #{_brojNaloga.Value}";
            UcitajPostojeceNaloge(_brojNaloga.Value);
        }
        else
        {
            Title = "Nova Prijava (Nalog)";
            // Autogenerate next Nalog ID based on max in Prijave
            var maxNalog = _db.Prijave.Any() ? _db.Prijave.Max(p => p.BrojNaloga) : 0;
            TxtBrojNaloga.Text = (maxNalog + 1).ToString();
        }
    }

    private void UcitajPostojeceNaloge(int brojNaloga)
    {
        var prijave = _db.Prijave
            .Include(p => p.Sredstvo)
            .Where(p => p.BrojNaloga == brojNaloga)
            .OrderBy(p => p.RedBroj)
            .ToList();

        if (prijave.Count == 0) return;

        var prva = prijave.First();
        TxtBrojNaloga.Text = prva.BrojNaloga.ToString();
        TxtBrojNaloga.IsReadOnly = true;
        CmbDobavljac.SelectedValue = prva.DobavljacId;
        DpDatum.SelectedDate = prva.DatumAktiviranja;

        foreach (var p in prijave)
        {
            Stavke.Add(new PrijavaStavkaViewModel
            {
                RedBroj = p.RedBroj,
                InventarskiBroj = p.InventarskiBroj,
                Naziv = p.Sredstvo?.Naziv ?? "Nepoznato",
                NabavnaVrednost = p.NabavnaVrednost,
                StopaAmortizacije = p.StopaAmortizacije,
                AmortizacionaGrupa = p.AmortizacionaGrupa1.ToString(),
                Konto = p.Konto,
                ObracunskaJedinica = p.ObracunskaJedinica,
                Kolicina = p.Kolicina,
                OtpisanaVrednost = p.OtpisanaVrednost,
                BrojFakture = p.BrojFakture,
                Sifra = p.Sredstvo?.LegacySifra ?? 0
            });
        }

        if (prva.Knjizen)
        {
            // Lock UI
            GridNovaStavka.IsEnabled = false;
            CmbDobavljac.IsEnabled = false;
            DpDatum.IsEnabled = false;
            BtnDodaj.Visibility = Visibility.Collapsed;
            BtnProknjizi.Visibility = Visibility.Collapsed;
            Title += " (PROKNJIŽENO)";
            MessageBox.Show("Ovaj nalog je već proknjižen i ne može se menjati.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnDodajStavku_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtInvBroj.Text) || string.IsNullOrWhiteSpace(TxtNaziv.Text))
        {
            MessageBox.Show("Inventarski broj i Naziv su obavezni.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtNabavna.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal nabavna))
        {
            MessageBox.Show("Neispravna nabavna vrednost.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (!decimal.TryParse(TxtStopa.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal stopa))
        {
            MessageBox.Show("Neispravna stopa amortizacije.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Check if InvBroj exists in DB or in current Stavke
        var invBroj = TxtInvBroj.Text.Trim();
        if (_db.Sredstva.Any(s => s.InventarskiBroj == invBroj) || Stavke.Any(s => s.InventarskiBroj == invBroj))
        {
            MessageBox.Show($"Sredstvo sa inventarskim brojem {invBroj} već postoji!", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int.TryParse(TxtOJ.Text.Trim(), out int oj);

        var novaStavka = new PrijavaStavkaViewModel
        {
            RedBroj = Stavke.Count + 1,
            InventarskiBroj = invBroj,
            Naziv = TxtNaziv.Text.Trim(),
            NabavnaVrednost = nabavna,
            StopaAmortizacije = stopa,
            AmortizacionaGrupa = TxtGrupa.Text.Trim(),
            Konto = TxtKonto.Text.Trim(),
            ObracunskaJedinica = oj,
            Kolicina = 1m, // Podrazumevana količina, kasnije možemo dodati unos na UI ako treba
            OtpisanaVrednost = 0m,
            BrojFakture = ""
        };

        Stavke.Add(novaStavka);
        ObrisiPoljaZaUnos();
    }

    private void ObrisiPoljaZaUnos()
    {
        TxtInvBroj.Text = "";
        TxtNaziv.Text = "";
        TxtNabavna.Text = "";
        TxtStopa.Text = "";
        TxtGrupa.Text = "";
        TxtKonto.Text = "";
        TxtOJ.Text = "";
        TxtInvBroj.Focus();
    }

    private void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (Stavke.Count == 0)
        {
            MessageBox.Show("Nalog nema nijednu stavku.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        if (!int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga))
        {
            MessageBox.Show("Neispravan broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            var datum = DpDatum.SelectedDate ?? DateTime.Today;
            int? dobId = CmbDobavljac.SelectedValue as int?;
            var firmaId = _db.Firme.FirstOrDefault()?.Id ?? 1;
            
            // Delete existing unposted records for this Nalog if any
            var postojece = _db.Prijave.Where(p => p.BrojNaloga == brojNaloga).ToList();
            if (postojece.Any())
            {
                _db.Prijave.RemoveRange(postojece);
            }

            foreach (var stavka in Stavke)
            {
                int.TryParse(stavka.AmortizacionaGrupa, out int amGrInt);

                var sredstvo = new Sredstvo
                {
                    InventarskiBroj = stavka.InventarskiBroj,
                    Naziv = stavka.Naziv,
                    DatumNabavke = datum,
                    DatumAktiviranja = datum,
                    NabavnaVrednost = stavka.NabavnaVrednost,
                    IspravkaVrednosti = 0,
                    SadasnjaVrednost = stavka.NabavnaVrednost,
                    StopaAmortizacije = stavka.StopaAmortizacije,
                    AmortizacionaGrupa = stavka.AmortizacionaGrupa,
                    JeAktivno = true,
                    LegacySifra = 0
                };
                _db.Sredstva.Add(sredstvo);
                _db.SaveChanges(); // Potrebno da dobijemo ID

                var kartica = new Kartica
                {
                    SredstvoId = sredstvo.Id,
                    RedBroj = 1,
                    Datum = datum,
                    OpisPromene = "Pocetno stanje / Nabavka",
                    Konto = stavka.Konto,
                    ObracunskaJedinica = stavka.ObracunskaJedinica,
                    NabavnaVrednost = stavka.NabavnaVrednost,
                    IspravkaVrednosti = 0,
                    StopaAmortizacije = stavka.StopaAmortizacije,
                    AmortizacionaGrupa1 = amGrInt,
                    KoeficijentRevalorizacije = 1
                };
                _db.Kartice.Add(kartica);

                var prijava = new Prijava
                {
                    BrojNaloga = brojNaloga,
                    RedBroj = stavka.RedBroj,
                    SredstvoId = sredstvo.Id,
                    DatumAktiviranja = datum,
                    InventarskiBroj = stavka.InventarskiBroj,
                    NabavnaVrednost = stavka.NabavnaVrednost,
                    StopaAmortizacije = stavka.StopaAmortizacije,
                    Konto = stavka.Konto,
                    ObracunskaJedinica = stavka.ObracunskaJedinica,
                    AmortizacionaGrupa1 = amGrInt,
                    Kolicina = stavka.Kolicina,
                    OtpisanaVrednost = stavka.OtpisanaVrednost,
                    BrojFakture = stavka.BrojFakture,
                    DobavljacId = dobId,
                    Knjizen = true
                };
                _db.Prijave.Add(prijava);
            }
            
            _db.SaveChanges();
            transaction.Commit();
            MessageBox.Show("Nalog je uspešno proknjižen!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            MessageBox.Show($"Greška prilikom knjiženja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        if (Stavke.Count == 0)
        {
            MessageBox.Show("Nema stavki za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga);
            var datum = DpDatum.SelectedDate ?? DateTime.Today;
            var dobavljac = CmbDobavljac.Text;
            var firma = _db.Firme.FirstOrDefault();
            var doc = new PrijavaDocument(brojNaloga, datum, dobavljac, Stavke, firma);
            
            var tempFile = Path.Combine(Path.GetTempPath(), $"Prijava_{brojNaloga}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            doc.GeneratePdf(tempFile);

            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška prilikom generisanja PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
