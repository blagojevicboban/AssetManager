using System.Windows;
using System.Windows.Controls;
using SredstvaApp.ViewModels;
using SredstvaData;

namespace SredstvaApp;

public partial class MainWindow : Window
{
    private readonly SredstvaDbContext _db;
    private Button? _activeNavButton;

    public MainWindow(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Prikaži ime firme
        var firma = _db.Firme.FirstOrDefault();
        ImeFirmeText.Text = firma?.Naziv ?? "—";

        // Otvori podrazumevanu stranicu
        NavigateTo(BtnSredstva, () => new Views.Sredstva.SredstvaPage(_db));
    }

    // ── Navigacija ────────────────────────────────────────────────
    private void NavigateTo(Button sender, Func<Page> pageFactory)
    {
        if (_activeNavButton != null)
            _activeNavButton.Style = FindResource("NavButton") as Style;

        sender.Style = FindResource("NavButtonActive") as Style;
        _activeNavButton = sender;

        MainFrame.Navigate(pageFactory());
    }

    // ── Sidebar dugmad ────────────────────────────────────────────
    private void BtnSredstva_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnSredstva, () => new Views.Sredstva.SredstvaPage(_db));

    private void BtnKartice_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnKartice, () => new Views.Kartice.KarticePage(_db));

    private void BtnPrijava_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnPrijava, () => new Views.Rashod.PrijavaPage(_db));

    private void BtnRashod_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnRashod, () => new Views.Rashod.RashodPage(_db));

    private void BtnAmortizacija_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnAmortizacija, () => new Views.Amortizacija.AmortizacijaPage(_db));

    private void BtnRevalorizacija_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnRevalorizacija, () => new Views.Revalorizacija.RevalorizacijaPage(_db));

    private void BtnPopis_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnPopis, () => new Views.Popis.PopisPage(_db));

    private void BtnRekap_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnRekap, () => new Views.Izvestaji.IzvestajiPage(_db));

    private void BtnDobavljaci_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnDobavljaci, () => new Views.Sifrarnici.DobavljaciPage(_db));

    private void BtnPodesavanja_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnPodesavanja, () => new Views.Sifrarnici.DobavljaciPage(_db));

    private void FirmaBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Placeholder — za budući dijalog za odabir firme
    }
}