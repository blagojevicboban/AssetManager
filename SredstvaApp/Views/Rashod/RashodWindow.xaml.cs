using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;
using SredstvaApp.Views.Rashod.Stampe;

namespace SredstvaApp.Views.Rashod;

public class RashodStavkaViewModel
{
    public int RedBroj { get; set; }
    public int SredstvoId { get; set; }
    public string InventarskiBroj { get; set; } = string.Empty;
    public string NazivSredstva { get; set; } = string.Empty;
    public TipoviPromena Tip { get; set; }
    public string TipTekst { get; set; } = string.Empty;
    public decimal Podaci { get; set; }
    public int ObracunskaJedinica { get; set; }
    // Za knjiženje
    public decimal NabavnaVrednostSredstva { get; set; }
    public decimal IspravkaVrednostiSredstva { get; set; }
    public decimal KolicinaSredstva { get; set; }
}

public partial class RashodWindow : Window
{
    private readonly SredstvaDbContext _db;
    private readonly int? _brojNaloga;
    public ObservableCollection<RashodStavkaViewModel> Stavke { get; } = new();

    // Novi nalog
    public RashodWindow(SredstvaDbContext db, int? brojNaloga)
    {
        InitializeComponent();
        _db = db;
        _brojNaloga = brojNaloga;
        DataContext = this;
        DgStavke.ItemsSource = Stavke;
        Loaded += RashodWindow_Loaded;
    }

    private void RashodWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Tipovi promene
        var tipovi = Enum.GetValues(typeof(TipoviPromena))
                         .Cast<TipoviPromena>()
                         .Select(t => new { Naziv = GetTipNaziv(t), Vrednost = t })
                         .ToList();
        CmbTipPromene.ItemsSource = tipovi;
        CmbTipPromene.SelectedIndex = 0;

        // Sredstva
        var sredstva = _db.Sredstva.Where(s => s.JeAktivno)
                                   .Select(s => new
                                   {
                                       s.Id,
                                       Prikaz = s.InventarskiBroj + " - " + s.Naziv,
                                       s.NabavnaVrednost,
                                       s.IspravkaVrednosti,
                                       s.Kolicina,
                                       s.InventarskiBroj,
                                       s.Naziv
                                   })
                                   .OrderBy(s => s.InventarskiBroj)
                                   .ToList();
        CmbSredstvo.ItemsSource = sredstva;

        if (_brojNaloga.HasValue)
        {
            // Pregled postojećeg naloga
            UcitajPostojeciNalog(_brojNaloga.Value);
        }
        else
        {
            // Novi nalog — auto broj
            var max = _db.Rashodi.Any() ? _db.Rashodi.Max(r => r.BrojNaloga) : 0;
            TxtBrojNaloga.Text = (max + 1).ToString();
            DpDatum.SelectedDate = DateTime.Today;
            Title = "Novi nalog rashoda";
            TxtTitle.Text = "Novi nalog rashoda";
        }
    }

    private void UcitajPostojeciNalog(int br)
    {
        var stavke = _db.Rashodi
            .Include(r => r.Sredstvo)
            .Where(r => r.BrojNaloga == br)
            .OrderBy(r => r.RedBroj)
            .ToList();

        if (!stavke.Any()) return;

        var prvi = stavke.First();
        TxtBrojNaloga.Text = br.ToString();
        TxtBrojNaloga.IsReadOnly = true;
        DpDatum.SelectedDate = prvi.Datum;
        TxtDokumentBroj.Text = prvi.DokumentBroj;
        Title = $"Nalog rashoda br. {br}";
        TxtTitle.Text = $"Nalog rashoda br. {br}";

        foreach (var r in stavke)
        {
            Stavke.Add(new RashodStavkaViewModel
            {
                RedBroj = r.RedBroj,
                SredstvoId = r.SredstvoId,
                InventarskiBroj = r.Sredstvo?.InventarskiBroj ?? r.SredstvoId.ToString(),
                NazivSredstva = r.Sredstvo?.Naziv ?? "—",
                Tip = r.Kod,
                TipTekst = r.KodTekst,
                Podaci = r.Podaci,
                ObracunskaJedinica = r.ObracunskaJedinica
            });
        }

        if (prvi.Knjizen)
        {
            GridNovaStavka.IsEnabled = false;
            BtnDodaj.Visibility = Visibility.Collapsed;
            BtnProknjizi.Visibility = Visibility.Collapsed;
            Title += " (PROKNJIŽENO)";
            TxtTitle.Text += " (PROKNJIŽENO)";
        }
    }

    private void CmbSredstvo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSredstvo.SelectedItem == null) return;
        dynamic s = CmbSredstvo.SelectedItem;
        decimal sadValue = (decimal)s.NabavnaVrednost - (decimal)s.IspravkaVrednosti;
        TxtSadasnja.Text = sadValue.ToString("N2");
    }

    private void CmbTipPromene_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTipPromene.SelectedItem == null) return;
        dynamic t = CmbTipPromene.SelectedItem;
        TipoviPromena tip = t.Vrednost;
        LblPodaci.Text = tip switch
        {
            TipoviPromena.KolicinskoRashodovanje or TipoviPromena.PovecanjeKolicine => "Količina",
            TipoviPromena.PrenosUDrugOJ => "Nova OJ (šifra)",
            TipoviPromena.PovecanjeVrednosti or TipoviPromena.PovecanjeAmortizacije => "Iznos povećanja",
            _ => "Vrednost izlaza"
        };
    }

    private void BtnDodaj_Click(object sender, RoutedEventArgs e)
    {
        if (CmbSredstvo.SelectedItem == null)
        {
            MessageBox.Show("Odaberite sredstvo.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(TxtPodaci.Text.Replace(",", "."), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal podaci))
        {
            MessageBox.Show("Neispravan unos za vrednost/podatke.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        dynamic sel = CmbSredstvo.SelectedItem;
        dynamic tipSel = CmbTipPromene.SelectedItem;
        TipoviPromena tip = tipSel.Vrednost;
        int sredstvoId = (int)sel.Id;

        if (Stavke.Any(s => s.SredstvoId == sredstvoId))
        {
            MessageBox.Show("Ovo sredstvo je već dodato u nalog.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Stavke.Add(new RashodStavkaViewModel
        {
            RedBroj = Stavke.Count + 1,
            SredstvoId = sredstvoId,
            InventarskiBroj = (string)sel.InventarskiBroj,
            NazivSredstva = (string)sel.Naziv,
            Tip = tip,
            TipTekst = GetTipNaziv(tip),
            Podaci = podaci,
            ObracunskaJedinica = 0,
            NabavnaVrednostSredstva = (decimal)sel.NabavnaVrednost,
            IspravkaVrednostiSredstva = (decimal)sel.IspravkaVrednosti,
            KolicinaSredstva = (decimal)sel.Kolicina
        });

        CmbSredstvo.SelectedIndex = -1;
        TxtPodaci.Text = "";
        TxtSadasnja.Text = "0.00";
    }

    private void BtnProknjizi_Click(object sender, RoutedEventArgs e)
    {
        if (Stavke.Count == 0)
        {
            MessageBox.Show("Nalog nema stavki.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(TxtBrojNaloga.Text, out int nalog))
        {
            MessageBox.Show("Neispravan broj naloga.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (DpDatum.SelectedDate == null)
        {
            MessageBox.Show("Unesite datum.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var datum = DpDatum.SelectedDate.Value;
        var dokument = TxtDokumentBroj.Text.Trim();

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            foreach (var stavka in Stavke)
            {
                var sredstvo = _db.Sredstva.Find(stavka.SredstvoId);
                if (sredstvo == null) continue;

                var poslednjaKartica = _db.Kartice
                    .Where(k => k.SredstvoId == stavka.SredstvoId)
                    .OrderByDescending(k => k.RedBroj)
                    .FirstOrDefault();

                int currentOj = poslednjaKartica?.ObracunskaJedinica ?? 0;
                decimal currentKolicina = poslednjaKartica?.Kolicina ?? 1;

                // Rashod zapis
                var maxRed = _db.Rashodi.Where(r => r.BrojNaloga == nalog).Select(r => (int?)r.RedBroj).Max() ?? 0;
                var rashod = new SredstvaData.Models.Rashod
                {
                    BrojNaloga = nalog,
                    RedBroj = maxRed + 1,
                    SredstvoId = stavka.SredstvoId,
                    Kod = stavka.Tip,
                    KodTekst = GetTipNaziv(stavka.Tip),
                    Datum = datum,
                    DokumentBroj = dokument,
                    Podaci = stavka.Podaci,
                    ObracunskaJedinica = currentOj,
                    Knjizen = true
                };
                _db.Rashodi.Add(rashod);

                // Kartica
                var maxKartica = _db.Kartice.Where(k => k.SredstvoId == stavka.SredstvoId).Select(k => (int?)k.RedBroj).Max() ?? 0;
                var kartica = new Kartica
                {
                    SredstvoId = stavka.SredstvoId,
                    RedBroj = maxKartica + 1,
                    Datum = datum,
                    Konto = poslednjaKartica?.Konto ?? string.Empty,
                    ObracunskaJedinica = currentOj,
                    AmortizacionaGrupa1 = poslednjaKartica?.AmortizacionaGrupa1 ?? 0,
                    AmortizacionaGrupa2 = poslednjaKartica?.AmortizacionaGrupa2 ?? 0,
                    StopaAmortizacije = sredstvo.StopaAmortizacije,
                    Kolicina = currentKolicina,
                    NabavnaVrednost = 0,
                    IspravkaVrednosti = 0,
                    KoeficijentRevalorizacije = 1
                };

                switch (stavka.Tip)
                {
                    case TipoviPromena.Rashodovanje:
                    case TipoviPromena.Prodaja:
                    case TipoviPromena.Otudjenje:
                    case TipoviPromena.Brisanje:
                        kartica.OpisPromene = "Storniranje - " + GetTipNaziv(stavka.Tip);
                        kartica.NabavnaVrednost = -sredstvo.NabavnaVrednost;
                        kartica.IspravkaVrednosti = -sredstvo.IspravkaVrednosti;
                        sredstvo.JeAktivno = false;
                        break;
                    case TipoviPromena.KolicinskoRashodovanje:
                        decimal proc = currentKolicina != 0 ? stavka.Podaci / currentKolicina : 0;
                        kartica.OpisPromene = "Kol. rashodovanje";
                        kartica.Kolicina = currentKolicina - stavka.Podaci;
                        kartica.NabavnaVrednost = -(sredstvo.NabavnaVrednost * proc);
                        kartica.IspravkaVrednosti = -(sredstvo.IspravkaVrednosti * proc);
                        sredstvo.NabavnaVrednost += kartica.NabavnaVrednost;
                        sredstvo.IspravkaVrednosti += kartica.IspravkaVrednosti;
                        break;
                    case TipoviPromena.PrenosUDrugOJ:
                        kartica.OpisPromene = "Prenos OJ na " + stavka.Podaci;
                        kartica.ObracunskaJedinica = (int)stavka.Podaci;
                        break;
                    case TipoviPromena.PovecanjeVrednosti:
                        kartica.OpisPromene = "Povećanje vrednosti";
                        kartica.NabavnaVrednost = stavka.Podaci;
                        sredstvo.NabavnaVrednost += stavka.Podaci;
                        break;
                    case TipoviPromena.PovecanjeKolicine:
                        kartica.OpisPromene = "Povećanje količine";
                        kartica.Kolicina = currentKolicina + stavka.Podaci;
                        break;
                    case TipoviPromena.PovecanjeAmortizacije:
                        kartica.OpisPromene = "Povećanje amortizacije";
                        kartica.IspravkaVrednosti = stavka.Podaci;
                        sredstvo.IspravkaVrednosti += stavka.Podaci;
                        break;
                }

                _db.Kartice.Add(kartica);
            }

            _db.SaveChanges();
            transaction.Commit();
            MessageBox.Show($"Nalog br. {nalog} je uspešno proknjižen! ({Stavke.Count} stavki)", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            MessageBox.Show("Greška pri knjiženju: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
            int.TryParse(TxtBrojNaloga.Text, out int nalogBr);
            var nazivFirme = _db.Firme.FirstOrDefault()?.Naziv ?? "Nepoznata firma";

            // Jedan nalog sa svim stavkama — kao originalni stamparashod() koji pravi novi list po nalogu
            var nalog = new RashodNalogInfo
            {
                BrojNaloga = nalogBr,
                Stavke = Stavke.Select(s => new RashodStavkaInfo
                {
                    Sifra = s.InventarskiBroj,
                    NazivSredstva = s.NazivSredstva,
                    OpisPromene = s.TipTekst,
                    Podaci = s.Podaci,
                    ObracunskaJedinica = s.ObracunskaJedinica,
                    Datum = DpDatum.SelectedDate ?? DateTime.Today,
                    DokumentBroj = TxtDokumentBroj.Text.Trim()
                }).ToList()
            };

            var doc = new RashodDocument(new System.Collections.Generic.List<RashodNalogInfo> { nalog }, nazivFirme);
            var tempFile = Path.Combine(Path.GetTempPath(), $"Rashod_{nalogBr}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            doc.GeneratePdf(tempFile);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZatvori_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private static string GetTipNaziv(TipoviPromena tip) => tip switch
    {
        TipoviPromena.Rashodovanje => "Rashodovanje",
        TipoviPromena.Prodaja => "Prodaja",
        TipoviPromena.Otudjenje => "Otuđenje",
        TipoviPromena.KolicinskoRashodovanje => "Količinsko rashodovanje",
        TipoviPromena.PrenosUDrugOJ => "Prenos u drugu OJ",
        TipoviPromena.Brisanje => "Brisanje",
        TipoviPromena.PovecanjeVrednosti => "Povećanje vrednosti",
        TipoviPromena.PovecanjeKolicine => "Povećanje količine",
        TipoviPromena.PovecanjeAmortizacije => "Povećanje amortizacije",
        _ => tip.ToString()
    };
}
