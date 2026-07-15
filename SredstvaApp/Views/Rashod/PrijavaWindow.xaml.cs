using System.Windows;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Rashod;

public partial class PrijavaWindow : Window
{
    private readonly SredstvaDbContext _db;

    public PrijavaWindow(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += PrijavaWindow_Loaded;
    }

    private void PrijavaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Učitavanje dobavljača za ComboBox
        CmbDobavljac.ItemsSource = _db.Dobavljaci.OrderBy(d => d.OpisKonta).ToList();

        // Inicijalizacija datuma na današnji dan
        DpNabavka.SelectedDate = DateTime.Today;
        DpAktiviranje.SelectedDate = DateTime.Today;
    }

    private void BtnOdustani_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        if (!Validacija()) return;

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            // 1. Kreiranje Sredstva
            decimal nabavna = decimal.Parse(TxtNabavna.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            decimal stopa = decimal.Parse(TxtStopa.Text.Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture);
            
            // Provera da li inventarski broj već postoji
            var invBroj = TxtInventarskiBroj.Text.Trim();
            if (_db.Sredstva.Any(s => s.InventarskiBroj == invBroj))
            {
                MessageBox.Show("Sredstvo sa tim inventarskim brojem već postoji!", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var firmaId = _db.Firme.FirstOrDefault()?.Id ?? 1;

            var sredstvo = new Sredstvo
            {
                InventarskiBroj = invBroj,
                Naziv = TxtNaziv.Text.Trim(),
                DatumNabavke = DpNabavka.SelectedDate ?? DateTime.Today,
                DatumAktiviranja = DpAktiviranje.SelectedDate ?? DateTime.Today,
                NabavnaVrednost = nabavna,
                IspravkaVrednosti = 0,
                SadasnjaVrednost = nabavna,
                StopaAmortizacije = stopa,
                AmortizacionaGrupa = TxtGrupa.Text.Trim(),
                JeAktivno = true,
                FirmaId = firmaId,
                LegacySifra = 0 // Novo sredstvo, nema staru šifru
            };
            
            _db.Sredstva.Add(sredstvo);
            _db.SaveChanges(); // Snimamo da bi dobili Sredstvo.Id

            // 2. Kreiranje prve Kartice (Početno stanje)
            int.TryParse(TxtOJ.Text.Trim(), out int oj);

            var kartica = new Kartica
            {
                SredstvoId = sredstvo.Id,
                RedBroj = 1,
                Datum = sredstvo.DatumAktiviranja,
                OpisPromene = "Pocetno stanje / Nabavka",
                Konto = TxtKonto.Text.Trim(),
                ObracunskaJedinica = oj,
                NabavnaVrednost = nabavna,
                IspravkaVrednosti = 0,
                StopaAmortizacije = stopa,
                AmortizacionaGrupa1 = 0, // Može se mapirati ako se unosi kao broj
                KoeficijentRevalorizacije = 1
            };

            _db.Kartice.Add(kartica);

            // 3. Kreiranje Prijave (Dokument)
            int.TryParse(TxtBrojNaloga.Text.Trim(), out int brojNaloga);

            var prijava = new SredstvaData.Models.Prijava
            {
                SredstvoId = sredstvo.Id,
                DatumAktiviranja = sredstvo.DatumAktiviranja,
                BrojNaloga = brojNaloga,
                InventarskiBroj = invBroj,
                NabavnaVrednost = nabavna,
                StopaAmortizacije = stopa,
                Konto = TxtKonto.Text.Trim(),
                ObracunskaJedinica = oj,
                Knjizen = true
            };

            if (CmbDobavljac.SelectedValue is int dobId)
            {
                prijava.DobavljacId = dobId;
            }

            _db.Prijave.Add(prijava);
            _db.SaveChanges();

            transaction.Commit();
            MessageBox.Show("Uspešno kreirano sredstvo, prijava i početno stanje u kartici.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            MessageBox.Show($"Došlo je do greške prilikom čuvanja: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool Validacija()
    {
        if (string.IsNullOrWhiteSpace(TxtBrojNaloga.Text)) { Warn("Unesite broj naloga."); return false; }
        if (string.IsNullOrWhiteSpace(TxtInventarskiBroj.Text)) { Warn("Unesite inventarski broj."); return false; }
        if (string.IsNullOrWhiteSpace(TxtNaziv.Text)) { Warn("Unesite naziv sredstva."); return false; }
        
        if (!decimal.TryParse(TxtNabavna.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            Warn("Nabavna vrednost mora biti ispravan broj."); 
            return false;
        }

        if (!decimal.TryParse(TxtStopa.Text.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            Warn("Stopa amortizacije mora biti ispravan broj."); 
            return false;
        }

        return true;
    }

    private void Warn(string msg) => MessageBox.Show(msg, "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
}
