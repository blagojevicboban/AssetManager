using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Sredstva;

public partial class SredstvaPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<Sredstvo> _all = new();

    public SredstvaPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += SredstvaPage_Loaded;
    }

    private void UpdateTotals(IEnumerable<Sredstvo> items)
    {
        var list = items.ToList();
        decimal nabavna = list.Sum(s => s.NabavnaVrednost);
        decimal ispravka = list.Sum(s => s.IspravkaVrednosti);
        decimal sadasnja = list.Sum(s => s.SadasnjaVrednost);

        TxtTotalNabavna.Text = nabavna.ToString("N2");
        TxtTotalIspravka.Text = ispravka.ToString("N2");
        TxtTotalSadasnja.Text = sadasnja.ToString("N2");
        
        SubtitleText.Text = $"Ukupno {list.Count} sredstava";
    }

    private void SredstvaPage_Loaded(object sender, RoutedEventArgs e)
    {
        _all = _db.Sredstva
            .OrderBy(s => s.LegacySifra)
            .ToList();

        SredstvaGrid.ItemsSource = _all;
        UpdateTotals(_all);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q))
        {
            SredstvaGrid.ItemsSource = _all;
            UpdateTotals(_all);
        }
        else
        {
            var filtered = _all.Where(s =>
                s.Naziv.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.LegacySifra.ToString().Contains(q)).ToList();
            SredstvaGrid.ItemsSource = filtered;
            UpdateTotals(filtered);
        }
    }

    private void SredstvaGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Priprema za buduće akcije na selekciji
    }

    private void SredstvaGrid_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (SredstvaGrid.SelectedItem is Sredstvo s)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.OpenAnalitickaKartica(s.Id);
            }
        }
    }

    private void BtnKartica_Click(object sender, RoutedEventArgs e)
    {
        if (SredstvaGrid.SelectedItem is Sredstvo s)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.OpenAnalitickaKartica(s.Id);
            }
        }
        else
        {
            MessageBox.Show("Izaberite sredstvo iz liste.", "Kartica", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnNalepnice_Click(object sender, RoutedEventArgs e)
    {
        var selected = _all.Where(s => s.IsSelected).ToList();
        
        if (selected.Count == 0)
        {
            MessageBox.Show("Niste izabrali nijedno sredstvo. Štiklirajte kućice u prvoj koloni za sredstva koja želite da štampate.", "Nalepnice", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        try 
        {
            var firma = _db.Firme.FirstOrDefault();
            var doc = new NalepniceDocument(selected, firma);
            var filePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"Nalepnice_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            QuestPDF.Fluent.GenerateExtensions.GeneratePdf(doc, filePath);
            
            var p = new System.Diagnostics.Process();
            p.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
            p.Start();
            
            // Opciono: ocisti selekciju
            foreach(var s in selected) s.IsSelected = false;
            SredstvaGrid.Items.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri generisanju nalepnica: {ex.Message}", "Greška", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void BtnNovo_Click(object sender, RoutedEventArgs e)
    {
        var win = new Views.Rashod.PrijavaWindow(_db);
        win.ShowDialog();
        
        // Osvježi podatke
        SredstvaPage_Loaded(this, new RoutedEventArgs());
    }
}
