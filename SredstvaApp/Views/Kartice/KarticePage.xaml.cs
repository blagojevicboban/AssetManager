using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Kartice;

public partial class KarticePage : Page
{
    private readonly SredstvaDbContext _db;
    private List<Sredstvo> _allSredstva = new();
    private readonly int? _initialSredstvoId;

    public KarticePage(SredstvaDbContext db, int? initialSredstvoId = null)
    {
        InitializeComponent();
        _db = db;
        _initialSredstvoId = initialSredstvoId;
        Loaded += KarticePage_Loaded;
    }

    private void KarticePage_Loaded(object sender, RoutedEventArgs e)
    {
        _allSredstva = _db.Sredstva
            .Include(s => s.Kartice)
            .OrderBy(s => s.LegacySifra)
            .ToList();

        SredstvaList.ItemsSource = _allSredstva;

        if (_initialSredstvoId.HasValue)
        {
            var target = _allSredstva.FirstOrDefault(s => s.Id == _initialSredstvoId.Value);
            if (target != null)
            {
                SredstvaList.SelectedItem = target;
                SredstvaList.ScrollIntoView(target);
            }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim();
        if (string.IsNullOrEmpty(q))
        {
            SredstvaList.ItemsSource = _allSredstva;
        }
        else
        {
            SredstvaList.ItemsSource = _allSredstva.Where(s =>
                s.LegacySifra.ToString().Contains(q) ||
                s.Naziv.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList().OrderBy(s => s.LegacySifra);
        }
    }

    private void SredstvaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SredstvaList.SelectedItem is Sredstvo sredstvo)
        {
            EmptyPane.Visibility = Visibility.Collapsed;
            DetailPane.Visibility = Visibility.Visible;
            UcitajKarticu(sredstvo);
        }
        else
        {
            EmptyPane.Visibility = Visibility.Visible;
            DetailPane.Visibility = Visibility.Collapsed;
        }
    }

    private void UcitajKarticu(Sredstvo sredstvo)
    {
        NaslovText.Text = sredstvo.Naziv;
        SubtitleText.Text = $"Inventarski br: {sredstvo.InventarskiBroj}  •  Sifra: {sredstvo.LegacySifra}";

        NabavnaText.Text = sredstvo.NabavnaVrednost.ToString("N2");
        IspravkaText.Text = sredstvo.IspravkaVrednosti.ToString("N2");
        SadasnjaText.Text = sredstvo.SadasnjaVrednost.ToString("N2");
        StopaText.Text = $"{sredstvo.StopaAmortizacije:N2} %";

        InvBrText.Text = sredstvo.InventarskiBroj;
        AmGrupaText.Text = sredstvo.AmortizacionaGrupa;
        DatumAktText.Text = sredstvo.DatumAktiviranja == DateTime.MinValue ? "—" : sredstvo.DatumAktiviranja.ToString("dd.MM.yyyy");

        var kartice = sredstvo.Kartice
            .OrderBy(k => k.Datum)
            .ThenBy(k => k.RedBroj)
            .ToList();

        BrojStavkiText.Text = kartice.Count.ToString();

        decimal kumulativnaNab = 0m;
        decimal kumulativnaOtp = 0m;
        var redovi = kartice.Select(k =>
        {
            kumulativnaNab += k.NabavnaVrednost;
            kumulativnaOtp += k.IspravkaVrednosti;
            return new KarticaRedViewModel(k, kumulativnaNab - kumulativnaOtp);
        }).ToList();

        KarticaGrid.ItemsSource = redovi;

        if (redovi.Count > 0)
        {
            KarticaGrid.ScrollIntoView(redovi.Last());
        }
    }

    private void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        if (SredstvaList.SelectedItem is not Sredstvo sredstvo)
        {
            MessageBox.Show("Molimo izaberite sredstvo za štampu.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var firma = _db.Firme.FirstOrDefault();
            var kartice = sredstvo.Kartice.OrderBy(k => k.Datum).ThenBy(k => k.RedBroj).ToList();

            var doc = new Stampe.AnalitickaKarticaDocument(sredstvo, kartice, firma);

            var tempFile = Path.Combine(Path.GetTempPath(), $"AnalitickaKartica_{sredstvo.InventarskiBroj}_{DateTime.Now:yyyyMMddHHmmss}.pdf");
            doc.GeneratePdf(tempFile);

            Process.Start(new ProcessStartInfo(tempFile) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška prilikom generisanja PDF-a: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
