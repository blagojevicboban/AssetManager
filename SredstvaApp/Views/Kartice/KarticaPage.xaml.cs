using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Kartice;

public partial class KarticaPage : Page
{
    private readonly SredstvaDbContext _db;
    private readonly int _sredstvoId;

    public KarticaPage(SredstvaDbContext db, int sredstvoId)
    {
        InitializeComponent();
        _db = db;
        _sredstvoId = sredstvoId;
        Loaded += KarticaPage_Loaded;
    }

    private void KarticaPage_Loaded(object sender, RoutedEventArgs e)
    {
        var sredstvo = _db.Sredstva
            .Include(s => s.Kartice)
            .FirstOrDefault(s => s.Id == _sredstvoId);

        if (sredstvo == null)
        {
            NaslovText.Text = "Greška — sredstvo nije pronađeno";
            return;
        }

        // Naslov
        NaslovText.Text = sredstvo.Naziv;
        SubtitleText.Text = $"Inventarski br: {sredstvo.InventarskiBroj}  •  Sifra: {sredstvo.LegacySifra}";

        // Stat kartice
        NabavnaText.Text = sredstvo.NabavnaVrednost.ToString("N2");
        IspravkaText.Text = sredstvo.IspravkaVrednosti.ToString("N2");
        SadasnjaText.Text = sredstvo.SadasnjaVrednost.ToString("N2");
        StopaText.Text = $"{sredstvo.StopaAmortizacije:N2} %";

        // Meta podaci
        InvBrText.Text = sredstvo.InventarskiBroj;
        AmGrupaText.Text = sredstvo.AmortizacionaGrupa;
        DatumAktText.Text = sredstvo.DatumAktiviranja == DateTime.MinValue
            ? "—"
            : sredstvo.DatumAktiviranja.ToString("dd.MM.yyyy");

        // Kartice sortirane hronološki
        var kartice = sredstvo.Kartice
            .OrderBy(k => k.Datum)
            .ThenBy(k => k.RedBroj)
            .ToList();

        BrojStavkiText.Text = kartice.Count.ToString();

        // Izračunaj kumulativnu sadašnju vrednost (nabavna - ispravka kumulativno)
        decimal kumulativnaNab = 0m;
        decimal kumulativnaOtp = 0m;
        var redovi = kartice.Select(k =>
        {
            kumulativnaNab += k.NabavnaVrednost;
            kumulativnaOtp += k.IspravkaVrednosti;
            return new KarticaRedViewModel(k, kumulativnaNab - kumulativnaOtp);
        }).ToList();

        KarticaGrid.ItemsSource = redovi;

        // Skroluj na kraj (najnovija promena)
        if (redovi.Count > 0)
        {
            KarticaGrid.ScrollIntoView(redovi[^1]);
        }
    }

    private void BtnNazad_Click(object sender, RoutedEventArgs e)
    {
        if (NavigationService?.CanGoBack == true)
            NavigationService.GoBack();
    }
}
