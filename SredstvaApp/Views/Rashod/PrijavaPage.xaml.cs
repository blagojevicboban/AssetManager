using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Rashod;

/// <summary>Red u listi prijava sa izvedenim prikaznim properijama.</summary>
public class PrijavaRedViewModel
{
    public int BrojNaloga { get; init; }
    public int SredstvoId { get; init; }
    public string NazivSredstva { get; init; } = string.Empty;
    public string InventarskiBroj { get; init; } = string.Empty;
    public string Konto { get; init; } = string.Empty;
    public int ObracunskaJedinica { get; init; }
    public DateTime DatumAktiviranja { get; init; }
    public decimal NabavnaVrednost { get; init; }
    public decimal StopaAmortizacije { get; init; }
    public bool Knjizen { get; init; }
    public string KnjizenTekst => Knjizen ? "✓ Da" : "◌ Ne";
}

public partial class PrijavaPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<PrijavaRedViewModel> _all = new();

    public PrijavaPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += PrijavaPage_Loaded;
    }

    private void PrijavaPage_Loaded(object sender, RoutedEventArgs e)
    {
        var prijave = _db.Prijave
            .Include(p => p.Sredstvo)
            .OrderBy(p => p.BrojNaloga)
            .ThenBy(p => p.RedBroj)
            .ToList();

        // Grupisanje: jedan red po nalogu (uzimamo prvu stavku naloga)
        _all = prijave
            .GroupBy(p => p.BrojNaloga)
            .Select(g =>
            {
                var first = g.First();
                return new PrijavaRedViewModel
                {
                    BrojNaloga = first.BrojNaloga,
                    SredstvoId = first.SredstvoId,
                    NazivSredstva = first.Sredstvo?.Naziv ?? "—",
                    InventarskiBroj = first.InventarskiBroj,
                    Konto = first.Konto,
                    ObracunskaJedinica = first.ObracunskaJedinica,
                    DatumAktiviranja = first.DatumAktiviranja,
                    NabavnaVrednost = first.NabavnaVrednost,
                    StopaAmortizacije = first.StopaAmortizacije,
                    Knjizen = first.Knjizen
                };
            })
            .ToList();

        PrijavaGrid.ItemsSource = _all;
        var proknjizeno = _all.Count(p => p.Knjizen);
        SubtitleText.Text = $"Ukupno {_all.Count} naloga  •  Proknjiženo: {proknjizeno}  •  Na čekanju: {_all.Count - proknjizeno}";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q))
        {
            PrijavaGrid.ItemsSource = _all;
        }
        else
        {
            PrijavaGrid.ItemsSource = _all.Where(p =>
                p.NazivSredstva.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.BrojNaloga.ToString().Contains(q) ||
                p.InventarskiBroj.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    private void PrijavaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrijavaGrid.SelectedItem is PrijavaRedViewModel p)
            StatusText.Text = $"Nalog #{p.BrojNaloga}: {p.NazivSredstva}  •  Dupli klik za prikaz kartice sredstva";
    }

    private void PrijavaGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PrijavaGrid.SelectedItem is PrijavaRedViewModel p)
        {
            NavigationService?.Navigate(new Views.Kartice.KarticaPage(_db, p.SredstvoId));
        }
    }

    private void BtnNova_Click(object sender, RoutedEventArgs e)
    {
        var w = new PrijavaWindow(_db);
        if (w.ShowDialog() == true)
        {
            // Osvežavamo listu nakon uspešne prijave
            PrijavaPage_Loaded(null!, null!);
        }
    }
}
