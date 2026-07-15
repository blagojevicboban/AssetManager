using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SredstvaData;

namespace SredstvaApp.Views.Rashod;

public class PrijavaRedViewModel
{
    public int BrojNaloga { get; init; }
    public DateTime DatumAktiviranja { get; init; }
    public int BrojStavki { get; init; }
    public decimal UkupnaNabavnaVrednost { get; init; }
    public bool Knjizen { get; init; }
    public string KnjizenTekst => Knjizen ? "✔️ Da" : "❌ Ne";
    public string DobavljacNaziv { get; init; } = string.Empty;
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
            .Include(p => p.Dobavljac)
            .OrderBy(p => p.BrojNaloga)
            .ThenBy(p => p.RedBroj)
            .ToList();

        _all = prijave
            .GroupBy(p => p.BrojNaloga)
            .Select(g =>
            {
                var first = g.First();
                return new PrijavaRedViewModel
                {
                    BrojNaloga = g.Key,
                    DatumAktiviranja = first.DatumAktiviranja,
                    BrojStavki = g.Count(),
                    UkupnaNabavnaVrednost = g.Sum(x => x.NabavnaVrednost),
                    Knjizen = first.Knjizen,
                    DobavljacNaziv = first.Dobavljac?.OpisKonta ?? "Nepoznat dobavljač"
                };
            })
            .ToList();

        PrijavaGrid.ItemsSource = _all;

        // Stat kartice
        var proknjizeno = _all.Count(p => p.Knjizen);
        StatUkupno.Text = _all.Count.ToString();
        StatStavki.Text = prijave.Count.ToString();
        StatUkupnaVrednost.Text = prijave.Sum(p => p.NabavnaVrednost).ToString("N2");
        StatKnjizeno.Text = proknjizeno.ToString();
        StatCekanje.Text = (_all.Count - proknjizeno).ToString();

        SubtitleText.Text = $"Ukupno {_all.Count} naloga  •  {prijave.Count} stavki  •  Proknjiženo: {proknjizeno}";
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
                p.BrojNaloga.ToString().Contains(q) ||
                p.DobavljacNaziv.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
        }
    }

    private void PrijavaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrijavaGrid.SelectedItem is PrijavaRedViewModel p)
            StatusText.Text = $"Nalog #{p.BrojNaloga}  •  Dupli klik za pregled naloga";
    }

    private void PrijavaGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PrijavaGrid.SelectedItem is PrijavaRedViewModel p)
        {
            var w = new PrijavaWindow(_db, p.BrojNaloga);
            if (w.ShowDialog() == true)
            {
                PrijavaPage_Loaded(null!, null!);
            }
        }
    }

    private void BtnNova_Click(object sender, RoutedEventArgs e)
    {
        var w = new PrijavaWindow(_db, null);
        if (w.ShowDialog() == true)
        {
            PrijavaPage_Loaded(null!, null!);
        }
    }
}
