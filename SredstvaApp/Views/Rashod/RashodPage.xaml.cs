using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Rashod;

/// <summary>Red u listi rashoda sa izvedenim prikaznim properijama.</summary>
public class RashodRedViewModel
{
    public int BrojNaloga { get; init; }
    public int SredstvoId { get; init; }
    public string NazivSredstva { get; init; } = string.Empty;
    public DateTime Datum { get; init; }
    public TipoviPromena Kod { get; init; }
    public string KodTekst { get; init; } = string.Empty;
    public string DokumentBroj { get; init; } = string.Empty;
    public decimal Podaci { get; init; }
    public int ObracunskaJedinica { get; init; }
    public bool Knjizen { get; init; }

    public string KnjizenTekst => Knjizen ? "✓ Da" : "◌ Ne";

    /// <summary>Boja znački za vrstu promene.</summary>
    public Brush TipBoja => Kod switch
    {
        TipoviPromena.Rashodovanje => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),   // crvena
        TipoviPromena.Prodaja => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),         // narandžasta
        TipoviPromena.Otudjenje => new SolidColorBrush(Color.FromRgb(0xF9, 0x73, 0x16)),       // tamno-narandžasta
        TipoviPromena.KolicinskoRashodovanje => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
        TipoviPromena.PrenosUDrugOJ => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)), // plava
        TipoviPromena.Brisanje => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),        // siva
        TipoviPromena.PovecanjeVrednosti => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)), // zelena
        TipoviPromena.PovecanjeKolicine => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
        TipoviPromena.PovecanjeAmortizacije => new SolidColorBrush(Color.FromRgb(0x8B, 0x5C, 0xF6)), // ljubičasta
        _ => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
    };
}

public partial class RashodPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<RashodRedViewModel> _all = new();

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
            .OrderByDescending(r => r.Datum)
            .ThenBy(r => r.BrojNaloga)
            .ToList();

        _all = rashodi.Select(r => new RashodRedViewModel
        {
            BrojNaloga = r.BrojNaloga,
            SredstvoId = r.SredstvoId,
            NazivSredstva = r.Sredstvo?.Naziv ?? "—",
            Datum = r.Datum,
            Kod = r.Kod,
            KodTekst = r.KodTekst.Length > 0 ? r.KodTekst : r.Kod.ToString(),
            DokumentBroj = r.DokumentBroj,
            Podaci = r.Podaci,
            ObracunskaJedinica = r.ObracunskaJedinica,
            Knjizen = r.Knjizen
        }).ToList();

        RashodGrid.ItemsSource = _all;

        // Stat kartice
        StatUkupno.Text = _all.GroupBy(r => r.BrojNaloga).Count().ToString();
        StatRashod.Text = _all.Count(r => r.Kod == TipoviPromena.Rashodovanje || r.Kod == TipoviPromena.KolicinskoRashodovanje).ToString();
        StatProdaja.Text = _all.Count(r => r.Kod == TipoviPromena.Prodaja).ToString();
        StatKnjizeno.Text = _all.Count(r => r.Knjizen).ToString();
        StatCekanje.Text = _all.Count(r => !r.Knjizen).ToString();

        SubtitleText.Text = $"Ukupno {_all.Count} stavki rashoda  •  {_all.GroupBy(r => r.BrojNaloga).Count()} naloga";

        // Filter po tipu
        var tipovi = new[] { "Svi tipovi" }
            .Concat(_all.Select(r => r.KodTekst).Distinct().OrderBy(t => t))
            .ToList();
        TipFilter.ItemsSource = tipovi;
        TipFilter.SelectedIndex = 0;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => Filter();
    private void TipFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => Filter();

    private void Filter()
    {
        var q = SearchBox.Text.Trim();
        var tip = TipFilter.SelectedItem as string;

        var filtered = _all.AsEnumerable();

        if (!string.IsNullOrEmpty(q))
            filtered = filtered.Where(r =>
                r.NazivSredstva.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.BrojNaloga.ToString().Contains(q) ||
                r.DokumentBroj.Contains(q, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(tip) && tip != "Svi tipovi")
            filtered = filtered.Where(r => r.KodTekst == tip);

        RashodGrid.ItemsSource = filtered.ToList();
    }

    private void RashodGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RashodGrid.SelectedItem is RashodRedViewModel r)
        {
            NavigationService?.Navigate(new Views.Kartice.KarticaPage(_db, r.SredstvoId));
        }
    }

    private void BtnNoviRashod_Click(object sender, RoutedEventArgs e)
    {
        var w = new RashodWindow(_db);
        if (w.ShowDialog() == true)
        {
            RashodPage_Loaded(null!, null!);
        }
    }
}
