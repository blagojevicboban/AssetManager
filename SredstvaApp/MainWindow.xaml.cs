using System.Windows;
using System.Windows.Controls;
using SredstvaApp.ViewModels;
using SredstvaData;
using Velopack;

namespace SredstvaApp;

public partial class MainWindow : Window
{
    private readonly SredstvaDbContext _db;
    private Button? _activeNavButton;

    public MainWindow(SredstvaDbContext db)
    {
        InitializeComponent();
        
        if (UserSettings.Instance.StartMaximized)
        {
            WindowState = WindowState.Maximized;
        }
        
        _db = db;
        
        UpdateUserInfo();
        ApplyRolePermissions();
        
        AppSession.TrenutnaFirmaChanged += () =>
        {
            Dispatcher.Invoke(() => 
            {
                ImeFirmeText.Text = AppSession.TrenutnaFirma?.Naziv ?? "—";
            });
        };
        ImeFirmeText.Text = AppSession.TrenutnaFirma?.Naziv ?? "—";
        
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        var versionStr = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        VersionText.Text = $"v{versionStr}  •  {System.DateTime.Now.Year}";
        
        // Prikazujemo prvu stranicu (Radna tabla)
        NavigateTo(BtnDashboard, () => new Views.Dashboard.DashboardPage(_db));

        // Provera ažuriranja u pozadini
        _ = CheckForUpdatesAsync();
    }

    private async System.Threading.Tasks.Task CheckForUpdatesAsync()
    {
        try
        {
            var source = new Velopack.Sources.GithubSource(
                "https://github.com/blagojevicboban/AssetManager",
                null, // null = javni repozitorijum, nema potrebe za tokenom
                false);
            var mgr = new UpdateManager(source);
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                var dialog = new UpdateDialog(newVersion, mgr);
                dialog.Owner = this;
                dialog.ShowDialog();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Greška pri proveri ažuriranja: {ex.Message}");
        }
    }

    private void UpdateUserInfo()
    {
        if (AppSession.TrenutniKorisnik != null)
        {
            TxtImeKorisnika.Text = AppSession.TrenutniKorisnik.ImePrezime;
            TxtUlogaKorisnika.Text = AppSession.TrenutniKorisnik.Uloga.ToString();
        }
    }

    private void ApplyRolePermissions()
    {
        // Gledalac ne sme da vrši promene ni obračune
        if (AppSession.TrenutniKorisnik?.Uloga == SredstvaData.Models.UlogaKorisnika.Gledalac)
        {
            BtnPrijava.Visibility = Visibility.Collapsed;
            BtnRashod.Visibility = Visibility.Collapsed;
            BtnAmortizacija.Visibility = Visibility.Collapsed;
            BtnRevalorizacija.Visibility = Visibility.Collapsed;
            BtnPodesavanja.Visibility = Visibility.Collapsed;
        }
        
        // Samo Administrator sme da vidi Korisnike i Podešavanja
        if (AppSession.TrenutniKorisnik?.Uloga != SredstvaData.Models.UlogaKorisnika.Administrator)
        {
            BtnKorisnici.Visibility = Visibility.Collapsed;
            BtnPodesavanja.Visibility = Visibility.Collapsed;
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Prikaži ime firme
        var firma = _db.Firme.FirstOrDefault();
        ImeFirmeText.Text = firma?.Naziv ?? "—";
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
    private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnDashboard, () => new Views.Dashboard.DashboardPage(_db));

    private void BtnSredstva_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnSredstva, () => new Views.Sredstva.SredstvaPage(_db));

    private void BtnKartice_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnKartice, () => new Views.Kartice.KarticePage(_db));

    public void OpenAnalitickaKartica(int sredstvoId)
    {
        NavigateTo(BtnKartice, () => new Views.Kartice.KarticePage(_db, sredstvoId));
    }

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
        => NavigateTo(BtnDobavljaci, () => new Views.Dobavljaci.DobavljaciPage(_db));

    private void FirmaBorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_activeNavButton != null)
        {
            _activeNavButton.Style = FindResource("NavButton") as Style;
            _activeNavButton = null;
        }
        MainFrame.Navigate(new Views.Firme.FirmePage());
    }

    private void BtnKorisnici_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnKorisnici, () => new Views.Korisnici.KorisniciPage(_db));

    private void BtnPodesavanja_Click(object sender, RoutedEventArgs e)
        => NavigateTo(BtnPodesavanja, () => new Views.Podesavanja.PodesavanjaPage());

    private void BtnOdjava_Click(object sender, RoutedEventArgs e)
    {
        AppSession.TrenutniKorisnik = null;
        var loginWindow = new SredstvaApp.Views.Korisnici.LoginWindow(_db);
        loginWindow.Show();
        this.Close();
    }

    private void BtnPomoc_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var helpPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Help", "uputstvo.html");
            if (System.IO.File.Exists(helpPath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = helpPath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show("Uputstvo nije pronađeno. Fajl ne postoji na putanji: " + helpPath, "Greška", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Nije moguće otvoriti uputstvo: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnChangelog_Click(object sender, RoutedEventArgs e)
    {
        PrikaziChangelog();
    }

    private void VersionText_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        PrikaziChangelog();
    }

    private void PrikaziChangelog()
    {
        var win = new Views.Pomoc.ChangelogWindow
        {
            Owner = this
        };
        win.ShowDialog();
    }
}