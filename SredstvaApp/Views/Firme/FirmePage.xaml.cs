using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Firme;

public class FirmaGridItem
{
    public int Id { get; set; }
    public string Naziv { get; set; } = string.Empty;
    public string Mesto { get; set; } = string.Empty;
    public string PIB { get; set; } = string.Empty;
    public string MaticniBroj { get; set; } = string.Empty;
    public string DbPath { get; set; } = string.Empty;
}

public partial class FirmePage : Page
{
    private List<FirmaGridItem> _allFirme = new();
    private ObservableCollection<FirmaGridItem> _displayedFirme = new();
    private SredstvaDbContext _db;

    public FirmePage(SredstvaDbContext currentDb)
    {
        InitializeComponent();
        _db = currentDb;
        Loaded += FirmePage_Loaded;
    }

    private void FirmePage_Loaded(object sender, RoutedEventArgs e)
    {
        UcitajPodatke();
    }

    private void UcitajPodatke()
    {
        try
        {
            var bazeDir = AppConfig.BazeDir;
            Directory.CreateDirectory(bazeDir);

            var dbFiles = Directory.GetFiles(bazeDir, "*.db");
            var firmeList = new List<FirmaGridItem>();

            foreach (var file in dbFiles)
            {
                try
                {
                    using var fileDb = SredstvaDbContext.Create(file);
                    var f = fileDb.Firme.FirstOrDefault();
                    if (f != null)
                    {
                        firmeList.Add(new FirmaGridItem
                        {
                            Id = f.Id,
                            Naziv = f.Naziv,
                            Mesto = f.Mesto,
                            PIB = f.PIB,
                            MaticniBroj = f.MaticniBroj,
                            DbPath = file
                        });
                    }
                }
                catch { }
            }

            _allFirme = firmeList.OrderBy(f => f.Naziv).ToList();
            OsveziTabelu();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri učitavanju firmi: {ex.Message}");
        }
    }

    private void OsveziTabelu()
    {
        var filter = SearchBox.Text.Trim().ToLower();
        List<FirmaGridItem> filtered;

        if (string.IsNullOrWhiteSpace(filter))
        {
            filtered = _allFirme;
            SearchPlaceholder.Visibility = Visibility.Visible;
        }
        else
        {
            filtered = _allFirme.Where(f => 
                f.Naziv.ToLower().Contains(filter) || 
                f.Mesto.ToLower().Contains(filter)
            ).ToList();
            SearchPlaceholder.Visibility = Visibility.Collapsed;
        }

        _displayedFirme = new ObservableCollection<FirmaGridItem>(filtered);
        FirmeGrid.ItemsSource = _displayedFirme;
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        OsveziTabelu();
    }

    private void FirmeGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void BtnPostaviAktivnu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is FirmaGridItem item)
        {
            if (AppConfig.DbPath == item.DbPath)
            {
                MessageBox.Show("Ova firma je već aktivna.", "Informacija", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            AppConfig.DbPath = item.DbPath;
            
            MessageBox.Show("Firma je uspešno promenjena. Da bi se promene primenile, aplikacija će se sada ponovo pokrenuti (simulacija).", 
                "Uspešno", MessageBoxButton.OK, MessageBoxImage.Information);
                
            // Restart aplikacije ili reload MainWindow-a
            System.Diagnostics.Process.Start(System.Reflection.Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));
            Application.Current.Shutdown();
        }
    }
}
