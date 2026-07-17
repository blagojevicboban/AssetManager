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

    private void SredstvaPage_Loaded(object sender, RoutedEventArgs e)
    {
        _all = _db.Sredstva
            .OrderBy(s => s.LegacySifra)
            .ToList();

        SredstvaGrid.ItemsSource = _all;
        SubtitleText.Text = $"Ukupno {_all.Count} sredstava";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(q))
        {
            SredstvaGrid.ItemsSource = _all;
        }
        else
        {
            SredstvaGrid.ItemsSource = _all.Where(s =>
                s.Naziv.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                s.LegacySifra.ToString().Contains(q)).ToList();
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

    private void BtnNovo_Click(object sender, RoutedEventArgs e)
    {
        var win = new Views.Rashod.PrijavaWindow(_db);
        win.ShowDialog();
        
        // Osvježi podatke
        SredstvaPage_Loaded(this, new RoutedEventArgs());
    }
}
