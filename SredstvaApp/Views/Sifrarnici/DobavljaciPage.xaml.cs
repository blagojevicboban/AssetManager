using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Sifrarnici;

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
        MessageBox.Show("Forma za unos novog dobavljača — u razvoju.", "Novi dobavljač",
            MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
