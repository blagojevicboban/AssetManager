using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Rashod;

public partial class RashodWindow : Window
{
    private readonly SredstvaDbContext _db;

    public RashodWindow(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += RashodWindow_Loaded;
    }

    private void RashodWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Učitaj tipove promene
        var tipovi = Enum.GetValues(typeof(TipoviPromena))
                         .Cast<TipoviPromena>()
                         .Select(t => new { Naziv = GetTipNaziv(t), Vrednost = t })
                         .ToList();
        CmbTipPromene.ItemsSource = tipovi;
        CmbTipPromene.SelectedIndex = 0;

        // Učitaj sredstva
        var sredstva = _db.Sredstva.Where(s => s.JeAktivno)
                                   .Select(s => new 
                                   { 
                                       s.Id, 
                                       Prikaz = s.InventarskiBroj + " - " + s.Naziv,
                                       s.NabavnaVrednost,
                                       s.IspravkaVrednosti
                                   })
                                   .OrderBy(s => s.Prikaz)
                                   .ToList();
        CmbSredstvo.ItemsSource = sredstva;

        DpDatum.SelectedDate = DateTime.Today;

        // Generisanje sledećeg broja naloga
        var maxNalog = _db.Rashodi.Any() ? _db.Rashodi.Max(r => r.BrojNaloga) : 0;
        TxtBrojNaloga.Text = (maxNalog + 1).ToString();
    }

    private string GetTipNaziv(TipoviPromena tip)
    {
        return tip switch
        {
            TipoviPromena.Rashodovanje => "1 - Rashodovanje (Potpuni otpis)",
            TipoviPromena.Prodaja => "2 - Prodaja",
            TipoviPromena.Otudjenje => "3 - Otuđenje",
            TipoviPromena.KolicinskoRashodovanje => "4 - Količinsko rashodovanje",
            TipoviPromena.PrenosUDrugOJ => "5 - Prenos u drugu OJ",
            TipoviPromena.Brisanje => "6 - Brisanje",
            TipoviPromena.PovecanjeVrednosti => "7 - Povećanje vrednosti",
            TipoviPromena.PovecanjeKolicine => "8 - Povećanje količine",
            TipoviPromena.PovecanjeAmortizacije => "9 - Povećanje amortizacije",
            _ => tip.ToString()
        };
    }

    private void CmbSredstvo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbSredstvo.SelectedItem != null)
        {
            dynamic s = CmbSredstvo.SelectedItem;
            TxtTrenutnaNabavna.Text = s.NabavnaVrednost.ToString("N2");
            TxtTrenutnaIspravka.Text = s.IspravkaVrednosti.ToString("N2");
            TxtTrenutnaSadasnja.Text = (s.NabavnaVrednost - s.IspravkaVrednosti).ToString("N2");
        }
        else
        {
            TxtTrenutnaNabavna.Text = "0.00";
            TxtTrenutnaIspravka.Text = "0.00";
            TxtTrenutnaSadasnja.Text = "0.00";
        }
    }

    private void CmbTipPromene_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbTipPromene.SelectedItem == null) return;

        dynamic t = CmbTipPromene.SelectedItem;
        TipoviPromena tip = t.Vrednost;

        switch (tip)
        {
            case TipoviPromena.Rashodovanje:
            case TipoviPromena.Prodaja:
            case TipoviPromena.Otudjenje:
            case TipoviPromena.Brisanje:
                LblPodaci.Text = "Vrednost izlaza";
                OpisPromeneInfo.Text = "Sredstvo će biti u potpunosti otpisano i deaktivirano. Iznos se beleži kao vrednost prodaje/otpisa.";
                break;
            case TipoviPromena.KolicinskoRashodovanje:
            case TipoviPromena.PovecanjeKolicine:
                LblPodaci.Text = "Količina";
                OpisPromeneInfo.Text = "Unesite količinu. Vrednosti će biti proporcionalno ažurirane na osnovu promene količine.";
                break;
            case TipoviPromena.PrenosUDrugOJ:
                LblPodaci.Text = "Nova OJ (Šifra)";
                OpisPromeneInfo.Text = "Unesite šifru nove Obračunske jedinice. Sredstvo prelazi u novu OJ.";
                break;
            case TipoviPromena.PovecanjeVrednosti:
            case TipoviPromena.PovecanjeAmortizacije:
                LblPodaci.Text = "Iznos povećanja";
                OpisPromeneInfo.Text = "Unesite finansijski iznos za povećanje.";
                break;
        }
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (CmbSredstvo.SelectedValue == null)
        {
            MessageBox.Show("Odaberite sredstvo.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(TxtBrojNaloga.Text, out int nalog))
        {
            MessageBox.Show("Neispravan broj naloga.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DpDatum.SelectedDate == null)
        {
            MessageBox.Show("Unesite datum.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(TxtPodaci.Text, out decimal podaci))
        {
            MessageBox.Show("Neispravan unos za vrednost/podatke.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        dynamic selTip = CmbTipPromene.SelectedItem;
        TipoviPromena tip = selTip.Vrednost;
        int sredstvoId = (int)CmbSredstvo.SelectedValue;

        var sredstvo = _db.Sredstva.Find(sredstvoId);
        if (sredstvo == null) return;

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            var poslednjaKartica = _db.Kartice.Where(k => k.SredstvoId == sredstvoId).OrderByDescending(k => k.RedBroj).FirstOrDefault();
            int currentOj = poslednjaKartica != null ? poslednjaKartica.ObracunskaJedinica : 0;
            decimal currentKolicina = poslednjaKartica != null ? poslednjaKartica.Kolicina : 1;

            // 1. Zapis u RASHOD
            var maxRed = _db.Rashodi.Where(r => r.BrojNaloga == nalog).Select(r => (int?)r.RedBroj).Max() ?? 0;
            var rashod = new SredstvaData.Models.Rashod
            {
                BrojNaloga = nalog,
                RedBroj = maxRed + 1,
                SredstvoId = sredstvoId,
                Kod = tip,
                KodTekst = GetTipNaziv(tip),
                Datum = DpDatum.SelectedDate.Value,
                DokumentBroj = TxtDokumentBroj.Text.Trim(),
                Podaci = podaci,
                ObracunskaJedinica = currentOj,
                Knjizen = true
            };
            _db.Rashodi.Add(rashod);

            // 2. Ažuriranje SREDSTVA i upis u KARTICU
            var maxKartica = _db.Kartice.Where(k => k.SredstvoId == sredstvoId).Select(k => (int?)k.RedBroj).Max() ?? 0;
            
            var kartica = new Kartica
            {
                SredstvoId = sredstvoId,
                RedBroj = maxKartica + 1,
                Datum = DpDatum.SelectedDate.Value,
                Konto = poslednjaKartica != null ? poslednjaKartica.Konto : string.Empty,
                ObracunskaJedinica = currentOj,
                AmortizacionaGrupa1 = poslednjaKartica != null ? poslednjaKartica.AmortizacionaGrupa1 : 0,
                AmortizacionaGrupa2 = poslednjaKartica != null ? poslednjaKartica.AmortizacionaGrupa2 : 0,
                StopaAmortizacije = sredstvo.StopaAmortizacije,
                Kolicina = currentKolicina,
                NabavnaVrednost = 0,
                IspravkaVrednosti = 0
            };

            switch (tip)
            {
                case TipoviPromena.Rashodovanje:
                case TipoviPromena.Prodaja:
                case TipoviPromena.Otudjenje:
                case TipoviPromena.Brisanje:
                    kartica.OpisPromene = "Storniranje - " + tip.ToString();
                    kartica.NabavnaVrednost = -sredstvo.NabavnaVrednost;
                    kartica.IspravkaVrednosti = -sredstvo.IspravkaVrednosti;
                    
                    sredstvo.JeAktivno = false;
                    break;

                case TipoviPromena.KolicinskoRashodovanje:
                    decimal procenatSmanjenja = currentKolicina != 0 ? podaci / currentKolicina : 0;
                    decimal smanjenjeNabavne = sredstvo.NabavnaVrednost * procenatSmanjenja;
                    decimal smanjenjeIspravke = sredstvo.IspravkaVrednosti * procenatSmanjenja;
                    
                    kartica.OpisPromene = "Kol. rashodovanje";
                    kartica.Kolicina = currentKolicina - podaci;
                    kartica.NabavnaVrednost = -smanjenjeNabavne;
                    kartica.IspravkaVrednosti = -smanjenjeIspravke;

                    sredstvo.NabavnaVrednost -= smanjenjeNabavne;
                    sredstvo.IspravkaVrednosti -= smanjenjeIspravke;
                    break;

                case TipoviPromena.PrenosUDrugOJ:
                    kartica.OpisPromene = "Prenos OJ na " + podaci.ToString();
                    kartica.ObracunskaJedinica = (int)podaci; // Nova OJ
                    break;

                case TipoviPromena.PovecanjeVrednosti:
                    kartica.OpisPromene = "Povećanje vrednosti";
                    kartica.NabavnaVrednost = podaci;
                    sredstvo.NabavnaVrednost += podaci;
                    break;

                case TipoviPromena.PovecanjeKolicine:
                    kartica.OpisPromene = "Povećanje količine";
                    kartica.Kolicina = currentKolicina + podaci;
                    break;

                case TipoviPromena.PovecanjeAmortizacije:
                    kartica.OpisPromene = "Povećanje amortizacije";
                    kartica.IspravkaVrednosti = podaci;
                    sredstvo.IspravkaVrednosti += podaci;
                    break;
            }

            _db.Kartice.Add(kartica);
            
            _db.SaveChanges();
            transaction.Commit();
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            MessageBox.Show("Greška pri čuvanju: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
