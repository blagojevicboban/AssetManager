using System.Windows;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Dobavljaci;

public partial class DobavljacWindow : Window
{
    private readonly SredstvaDbContext _db;
    public bool Uspesno { get; private set; }
    public int NoviDobavljacId { get; private set; }
    private int? _editingId = null;

    // Konstruktor za dodavanje novog dobavljača
    public DobavljacWindow(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Title = "Novi dobavljač";
        UpdateUI(false);
    }

    // Konstruktor za izmenu postojećeg dobavljača
    public DobavljacWindow(SredstvaDbContext db, int dobavljacId)
    {
        InitializeComponent();
        _db = db;
        _editingId = dobavljacId;
        Title = "Izmena dobavljača";
        UpdateUI(true);

        Loaded += (s, e) => LoadDobavljacData(dobavljacId);
    }

    private void UpdateUI(bool isEditing)
    {
        if (isEditing)
        {
            TitleText.Text = "Izmena dobavljača";
            BtnSacuvaj.Content = "Sačuvaj izmene";
        }
        else
        {
            TitleText.Text = "Novi dobavljač";
            BtnSacuvaj.Content = "Dodaj";
        }
    }

    private void LoadDobavljacData(int dobavljacId)
    {
        var dobavljac = _db.Dobavljaci.FirstOrDefault(d => d.Id == dobavljacId);
        if (dobavljac == null) return;

        TxtKonto.Text = dobavljac.Konto.ToString();
        TxtOpisKonta.Text = dobavljac.OpisKonta;
        TxtUlica.Text = dobavljac.UlicaIBroj;
        TxtMesto.Text = dobavljac.MestoIBroj;

        // Zaključaj konto polje jer se čuva kao jedinstveni identifikator
        TxtKonto.IsEnabled = false;
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        Uspesno = false;
        DialogResult = false;
        Close();
    }

    private void BtnDodaj_Click(object sender, RoutedEventArgs e)
    {
        // Validacija
        if (string.IsNullOrWhiteSpace(TxtKonto.Text))
        {
            MessageBox.Show("Konto je obavezno polje.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TxtOpisKonta.Text))
        {
            MessageBox.Show("Opis konta (naziv dobavljača) je obavezno.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validacija konta - mora biti broj
        if (!int.TryParse(TxtKonto.Text.Trim(), out int konto))
        {
            MessageBox.Show("Konto mora biti broj.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Provera da li konto već postoji (ali ne za izmenu)
        if (_editingId == null && _db.Dobavljaci.Any(d => d.Konto == konto))
        {
            MessageBox.Show($"Dobavljač sa kontom {konto} već postoji.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Za izmenu, provera da li konto postoji na drugom dobavljaču
        if (_editingId != null && _db.Dobavljaci.Any(d => d.Konto == konto && d.Id != _editingId))
        {
            MessageBox.Show($"Dobavljač sa kontom {konto} već postoji.", "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (_editingId == null)
            {
                // Dodaj novi dobavljač
                var noviDobavljac = new Dobavljac
                {
                    Konto = konto,
                    OpisKonta = TxtOpisKonta.Text.Trim(),
                    UlicaIBroj = TxtUlica.Text.Trim(),
                    MestoIBroj = TxtMesto.Text.Trim()
                };

                _db.Dobavljaci.Add(noviDobavljac);
                _db.SaveChanges();
                NoviDobavljacId = noviDobavljac.Id;
                MessageBox.Show("Dobavljač je uspešno dodan.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // Izmeni postojeći dobavljač
                var dobavljac = _db.Dobavljaci.FirstOrDefault(d => d.Id == _editingId);
                if (dobavljac != null)
                {
                    dobavljac.OpisKonta = TxtOpisKonta.Text.Trim();
                    dobavljac.UlicaIBroj = TxtUlica.Text.Trim();
                    dobavljac.MestoIBroj = TxtMesto.Text.Trim();

                    _db.Dobavljaci.Update(dobavljac);
                    _db.SaveChanges();
                    MessageBox.Show("Dobavljač je uspešno izmenjen.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            Uspesno = true;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri čuvanju dobavljača: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
