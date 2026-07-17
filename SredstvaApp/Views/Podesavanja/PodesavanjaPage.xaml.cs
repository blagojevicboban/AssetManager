using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using SredstvaApp.Services;

namespace SredstvaApp.Views.Podesavanja;

public partial class PodesavanjaPage : Page
{
    public PodesavanjaPage()
    {
        InitializeComponent();

        try
        {
            TxtAktivnaBazaPath.Text = AppConfig.DbPath;
            var dbName = !string.IsNullOrEmpty(AppConfig.DbPath) ? Path.GetFileNameWithoutExtension(AppConfig.DbPath) : "sredstva";
            TxtPredlozenoIme.Text = $"{dbName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        }
        catch { }

        ChkStartMaximized.IsChecked = UserSettings.Instance.StartMaximized;

        CmbAutoBackup.SelectedIndex = UserSettings.Instance.AutoBackupFrequency >= 0 && UserSettings.Instance.AutoBackupFrequency <= 2 
            ? UserSettings.Instance.AutoBackupFrequency 
            : 1;

        OsveziIstorijuKopija();
    }

    private void ChkStartMaximized_Changed(object sender, RoutedEventArgs e)
    {
        if (ChkStartMaximized.IsChecked.HasValue)
        {
            UserSettings.Instance.StartMaximized = ChkStartMaximized.IsChecked.Value;
            UserSettings.Instance.Save();
        }
    }

    private void CmbAutoBackup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbAutoBackup.SelectedIndex >= 0)
        {
            UserSettings.Instance.AutoBackupFrequency = CmbAutoBackup.SelectedIndex;
            UserSettings.Instance.Save();
        }
    }

    private void OsveziIstorijuKopija()
    {
        try
        {
            var kopije = BackupService.Instance.UcitajIstorijuKopija();
            LstIstorijaKopija.ItemsSource = kopije;
        }
        catch { }
    }

    private void BtnKreirajBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dbPath = AppConfig.DbPath;
            if (!File.Exists(dbPath))
            {
                MessageBox.Show("Aktivna baza podataka ne postoji na navedenoj putanji!", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dbName = !string.IsNullOrEmpty(dbPath) ? Path.GetFileNameWithoutExtension(dbPath) : "sredstva";
            var dialog = new SaveFileDialog
            {
                Title = "Sačuvaj rezervnu kopiju baze podataka",
                Filter = "SQLite baza podataka (*.db)|*.db|Sve datoteke (*.*)|*.*",
                FileName = $"{dbName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                DefaultExt = ".db"
            };

            if (dialog.ShowDialog() == true)
            {
                BackupService.Instance.NapraviRucniBackup(dialog.FileName);
                StatusMessage.Text = $"Rezervna kopija je uspešno sačuvana na: {dialog.FileName}";
                MessageBox.Show($"Rezervna kopija baze podataka je uspešno kreirana!", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                
                var dbNameNew = !string.IsNullOrEmpty(AppConfig.DbPath) ? Path.GetFileNameWithoutExtension(AppConfig.DbPath) : "sredstva";
                TxtPredlozenoIme.Text = $"{dbNameNew}_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
                OsveziIstorijuKopija();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Došlo je do greške prilikom kreiranja rezervne kopije:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage.Text = "Greška pri kreiranju rezervne kopije.";
        }
    }

    private void BtnIzaberiVrati_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Izaberi rezervnu kopiju za vraćanje",
            Filter = "SQLite baza podataka (*.db)|*.db|Sve datoteke (*.*)|*.*",
            DefaultExt = ".db"
        };

        if (dialog.ShowDialog() == true)
        {
            IzvrsiVracanje(dialog.FileName);
        }
    }

    private void BtnVratiIzIstorije_Click(object sender, RoutedEventArgs e)
    {
        if (LstIstorijaKopija.SelectedItem is BackupItem selektovanaKopija)
        {
            IzvrsiVracanje(selektovanaKopija.Putanja);
        }
        else
        {
            MessageBox.Show("Molimo izaberite rezervnu kopiju iz tabele.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void IzvrsiVracanje(string sourcePath)
    {
        var result = MessageBox.Show(
            "Da li ste sigurni da želite da prepišete aktivnu bazu podataka iz ove rezervne kopije?\n" +
            "Svi trenutni podaci biće obrisani (biće napravljena sigurnosna kopija pre operacije).\n\n" +
            $"Fajl za vraćanje: {Path.GetFileName(sourcePath)}",
            "Potvrda vraćanja", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                if (BackupService.Instance.VratiBackup(sourcePath, out string errorMsg))
                {
                    MessageBox.Show("Baza podataka je uspešno vraćena! Aplikacija će se sada ponovo pokrenuti radi primene promena.", 
                        "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Restart
                    System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));
                    Application.Current.Shutdown();
                }
                else
                {
                    MessageBox.Show($"Greška pri vraćanju: {errorMsg}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritična greška: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnOtvoriFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = BackupService.Instance.BackupDir;
            if (Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
            else
            {
                MessageBox.Show("Folder sa rezervnim kopijama još ne postoji.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nije moguće otvoriti folder: {ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnIzaberiStariFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Izaberite folder starog programa (npr. C:\\arhibEL\\SREDSTVA)"
        };

        if (dialog.ShowDialog() == true)
        {
            TxtStarProgramFolder.Text = dialog.FolderName;
            try
            {
                var firme = DbfImportService.Instance.UcitajFirme(dialog.FolderName);
                LstFirmeIzStarogPrograma.ItemsSource = firme;
                LstFirmeIzStarogPrograma.Visibility = Visibility.Visible;
                StatusMessage.Text = $"Pronađeno firmi: {firme.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Greška pri čitanju KORISNIC.DBF:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                LstFirmeIzStarogPrograma.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void BtnUveziFirmu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is DbfFirmaDto firma)
        {
            var msg = MessageBox.Show($"Da li ste sigurni da želite da uvezete firmu '{firma.Naziv}'?\nProces može potrajati.", "Potvrda uvoza", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (msg == MessageBoxResult.Yes)
            {
                try
                {
                    StatusMessage.Text = "Uvoz u toku... Molimo sačekajte.";
                    // Može se uraditi asinhrono da ne blokira UI
                    var novaBazaPath = DbfImportService.Instance.ImportFirma(firma);
                    
                    MessageBox.Show($"Firma '{firma.Naziv}' je uspešno uvezena u bazu:\n{novaBazaPath}", "Uvoz završen", MessageBoxButton.OK, MessageBoxImage.Information);
                    StatusMessage.Text = "Uvoz uspešno završen.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Greška pri uvozu firme:\n{ex.Message}", "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusMessage.Text = "Greška pri uvozu.";
                }
            }
        }
    }
}
