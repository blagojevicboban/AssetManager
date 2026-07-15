using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;
using SredstvaApp.Views.Rashod.Stampe;

namespace SredstvaApp.Views.Rashod;

/// <summary>Red u listi rashod naloga — jedan red = jedan nalog.</summary>
public class RashodNalogViewModel
{
    public int BrojNaloga { get; init; }
    public DateTime Datum { get; init; }
    public string DokumentBroj { get; init; } = string.Empty;
    public int BrojStavki { get; init; }
    public decimal UkupnoPodaci { get; init; }
    public bool Knjizen { get; init; }
    public string KnjizenTekst => Knjizen ? "✓ Da" : "◌ Ne";

    // Dominant tip promene za prikaz
    public string DominantanTip { get; init; } = string.Empty;

    public Brush TipBoja => DominantanTip switch
    {
        "Rashodovanje" or "Količinsko rashodovanje" => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
        "Prodaja" => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
        "Otuđenje" => new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16)),
        "Prenos u drugu OJ" => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
        "Brisanje" => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
        "Povećanje vrednosti" or "Povećanje količine" => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
        "Povećanje amortizacije" => new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6)),
        _ => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
    };
}

public partial class RashodPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<RashodNalogViewModel> _all = new();

    public RashodPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += RashodPage_Loaded;
    }

    private void RashodPage_Loaded(object sender, RoutedEventArgs e)
    {
        var rashodi = _db.Rashodi
            .Include(r => r.Sredstvo)
            .OrderBy(r => r.BrojNaloga)
            .ThenBy(r => r.RedBroj)
            .ToList();

        _all = rashodi
            .GroupBy(r => r.BrojNaloga)
            .Select(g =>
            {
                var first = g.First();
                var dominantanTip = g
                    .GroupBy(r => r.KodTekst)
                    .OrderByDescending(t => t.Count())
                    .First().Key;
                return new RashodNalogViewModel
                {
                    BrojNaloga = g.Key,
                    Datum = first.Datum,
                    DokumentBroj = first.DokumentBroj,
                    BrojStavki = g.Count(),
                    UkupnoPodaci = g.Sum(r => r.Podaci),
                    Knjizen = first.Knjizen,
                    DominantanTip = dominantanTip
                };
            }).ToList();

        RashodGrid.ItemsSource = _all;

        // Stat kartice
        StatUkupno.Text = _all.Count.ToString();
        StatRashod.Text = rashodi.Count(r => r.Kod == TipoviPromena.Rashodovanje || r.Kod == TipoviPromena.KolicinskoRashodovanje).ToString();
        StatProdaja.Text = rashodi.Count(r => r.Kod == TipoviPromena.Prodaja).ToString();
        StatKnjizeno.Text = _all.Count(r => r.Knjizen).ToString();
        StatCekanje.Text = _all.Count(r => !r.Knjizen).ToString();

        SubtitleText.Text = $"Ukupno {_all.Count} naloga  •  {rashodi.Count} stavki";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Filter();

    private void Filter()
    {
        var q = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q))
            RashodGrid.ItemsSource = _all;
        else
            RashodGrid.ItemsSource = _all.Where(r =>
                r.BrojNaloga.ToString().Contains(q) ||
                r.DokumentBroj.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.DominantanTip.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private void RashodGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RashodGrid.SelectedItem is RashodNalogViewModel r)
        {
            var w = new RashodWindow(_db, r.BrojNaloga);
            if (w.ShowDialog() == true)
                RashodPage_Loaded(null!, null!);
        }
    }

    private void BtnNoviRashod_Click(object sender, RoutedEventArgs e)
    {
        var w = new RashodWindow(_db, null);
        if (w.ShowDialog() == true)
            RashodPage_Loaded(null!, null!);
    }

    private void BtnStampa_Click(object sender, RoutedEventArgs e)
    {
        var trenutniPrikaz = (RashodGrid.ItemsSource as List<RashodNalogViewModel>) ?? _all;
        if (!trenutniPrikaz.Any())
        {
            MessageBox.Show("Nema podataka za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var nazivFirme = _db.Firme.FirstOrDefault()?.Naziv ?? "Nepoznata firma";

            var brajevi = trenutniPrikaz.Select(n => n.BrojNaloga).ToHashSet();
            var rashodi = _db.Rashodi
                .Include(r => r.Sredstvo)
                .Where(r => brajevi.Contains(r.BrojNaloga))
                .OrderBy(r => r.BrojNaloga)
                .ThenBy(r => r.RedBroj)
                .ToList();

            var nalozi = rashodi
                .GroupBy(r => r.BrojNaloga)
                .OrderBy(g => g.Key)
                .Select(g => new RashodNalogInfo
                {
                    BrojNaloga = g.Key,
                    Stavke = g.Select(r => new RashodStavkaInfo
                    {
                        Sifra = r.Sredstvo?.InventarskiBroj ?? r.SredstvoId.ToString(),
                        NazivSredstva = r.Sredstvo?.Naziv ?? "—",
                        OpisPromene = r.KodTekst,
                        Podaci = r.Podaci,
                        ObracunskaJedinica = r.ObracunskaJedinica,
                        Datum = r.Datum,
                        DokumentBroj = r.DokumentBroj
                    }).ToList()
                }).ToList();

            var doc = new RashodDocument(nalozi, nazivFirme);
            var tempFile = Path.Combine(Path.GetTempPath(), $"Rashod_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            doc.GeneratePdf(tempFile);
            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
