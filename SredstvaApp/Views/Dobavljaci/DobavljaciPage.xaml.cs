using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Dobavljaci;

public class DobavljacRedViewModel
{
    public int Id { get; init; }
    public int Konto { get; init; }
    public string OpisKonta { get; init; } = string.Empty;
    public string UlicaIBroj { get; init; } = string.Empty;
    public string MestoIBroj { get; init; } = string.Empty;
    public int BrojPrijava { get; init; }
}

public class PrijavaMinViewModel
{
    public string Naziv { get; init; } = string.Empty;
    public decimal NabavnaVrednost { get; init; }
    public string NabavnaStr => NabavnaVrednost.ToString("N2");
}

public partial class DobavljaciPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<DobavljacRedViewModel> _all = new();
    private int _selectedDobavljacId = 0;

    public DobavljaciPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += DobavljaciPage_Loaded;
    }

    private void DobavljaciPage_Loaded(object sender, RoutedEventArgs e)
    {
        var dobavljaci = _db.Dobavljaci
            .Include(d => d.Prijave)
                .ThenInclude(p => p.Sredstvo)
            .OrderBy(d => d.Konto)
            .ToList();

        _all = dobavljaci.Select(d => new DobavljacRedViewModel
        {
            Id = d.Id,
            Konto = d.Konto,
            OpisKonta = d.OpisKonta,
            UlicaIBroj = d.UlicaIBroj,
            MestoIBroj = d.MestoIBroj,
            BrojPrijava = d.Prijave.Count
        }).ToList();

        DobavljaciGrid.ItemsSource = _all;
        SubtitleText.Text = $"Ukupno {_all.Count} dobavljača u šifarniku";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim();
        DobavljaciGrid.ItemsSource = string.IsNullOrEmpty(q)
            ? _all
            : _all.Where(d =>
                d.OpisKonta.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                d.Konto.ToString().Contains(q)).ToList();
    }

    private void DobavljaciGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DobavljaciGrid.SelectedItem is not DobavljacRedViewModel sel) return;

        _selectedDobavljacId = sel.Id;

        var dobavljac = _db.Dobavljaci
            .Include(d => d.Prijave)
                .ThenInclude(p => p.Sredstvo)
            .FirstOrDefault(d => d.Id == sel.Id);

        if (dobavljac == null) return;

        // Prikaži detalje
        DetailPlaceholder.Visibility = Visibility.Collapsed;
        DetailContent.Visibility = Visibility.Visible;

        DetailKonto.Text = dobavljac.Konto.ToString();
        DetailNaziv.Text = dobavljac.OpisKonta;
        DetailAdresa.Text = string.IsNullOrWhiteSpace(dobavljac.UlicaIBroj) ? "—" : dobavljac.UlicaIBroj;
        DetailMesto.Text = string.IsNullOrWhiteSpace(dobavljac.MestoIBroj) ? "" : dobavljac.MestoIBroj;

        // Lista prijava
        var prijave = dobavljac.Prijave
            .GroupBy(p => p.BrojNaloga)
            .Select(g => new PrijavaMinViewModel
            {
                Naziv = g.First().Sredstvo?.Naziv ?? $"Nalog #{g.Key}",
                NabavnaVrednost = g.First().NabavnaVrednost
            })
            .OrderBy(p => p.Naziv)
            .ToList();

        PrijaveList.ItemsSource = prijave;
    }

    private void BtnNovi_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DobavljacWindow(_db);
        if (dialog.ShowDialog() == true && dialog.Uspesno)
        {
            // Osvežimo listu dobavljača
            var handler = DobavljaciPage_Loaded;
            handler?.Invoke(this, new RoutedEventArgs());
        }
    }

    private void BtnIzmeni_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDobavljacId == 0)
        {
            MessageBox.Show("Molimo odaberite dobavljača koji želite da izmenite.", "Izmena dobavljača",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new DobavljacWindow(_db, _selectedDobavljacId);
        if (dialog.ShowDialog() == true && dialog.Uspesno)
        {
            // Osvežimo listu dobavljača
            _selectedDobavljacId = 0;
            DetailContent.Visibility = Visibility.Collapsed;
            DetailPlaceholder.Visibility = Visibility.Visible;
            var handler = DobavljaciPage_Loaded;
            handler?.Invoke(this, new RoutedEventArgs());
        }
    }

    private void BtnObrisi_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedDobavljacId == 0)
        {
            MessageBox.Show("Molimo odaberite dobavljača koji želite da obrišete.", "Brisanje dobavljača",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dobavljac = _db.Dobavljaci
            .Include(d => d.Prijave)
            .FirstOrDefault(d => d.Id == _selectedDobavljacId);
        if (dobavljac == null) return;

        // Provera da li dobavljač ima povezane prijave
        if (dobavljac.Prijave.Any())
        {
            MessageBox.Show(
                $"Nije moguće obrisati dobavljača \"{dobavljac.OpisKonta}\" jer ima povezanih prijava sredstava.\n\n" +
                $"Broj povezanih prijava: {dobavljac.Prijave.Count}\n\n" +
                $"Obrišite prvo sve prijave povezane sa ovim dobavljačem.",
                "Brisanje onemogućeno", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Da li ste sigurni da želite da obrišete dobavljača \"{dobavljac.OpisKonta}\"?\n\n" +
            $"Ova akcija se ne može poništiti.",
            "Potvrda brisanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _db.Dobavljaci.Remove(dobavljac);
                _db.SaveChanges();

                MessageBox.Show("Dobavljač je uspešno obrisan.", "Uspeh", 
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _selectedDobavljacId = 0;
                DetailContent.Visibility = Visibility.Collapsed;
                DetailPlaceholder.Visibility = Visibility.Visible;
                var handler = DobavljaciPage_Loaded;
            handler?.Invoke(this, new RoutedEventArgs());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri brisanju dobavljača: {ex.Message}", "Greška",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
