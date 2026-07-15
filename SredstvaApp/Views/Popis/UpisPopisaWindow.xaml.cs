using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Popis;

public partial class UpisPopisaWindow : Window
{
    private readonly int _popisId;
    private readonly SredstvaDbContext _db;
    private SredstvaData.Models.Popis? _popis;

    public UpisPopisaWindow(int popisId, SredstvaDbContext db)
    {
        InitializeComponent();
        _popisId = popisId;
        _db = db;
        
        Loaded += UpisPopisaWindow_Loaded;
    }

    private void UpisPopisaWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadData();
    }

    private void LoadData()
    {
        _popis = _db.Popisi
            .Include(p => p.Komisija)
            .Include(p => p.Stavke)
                .ThenInclude(s => s.Sredstvo)
            .FirstOrDefault(p => p.Id == _popisId)!;

        if (_popis == null) return;

        TxtNaslov.Text = $"Popisna lista {_popis.Id} / {_popis.Godina}";
        TxtPodaci.Text = $"Komisija: {_popis.Komisija.Naziv} | Datum popisa: {_popis.DatumPopisa:dd.MM.yyyy}";

        StavkeGrid.ItemsSource = _popis.Stavke.OrderBy(s => s.Sredstvo.InventarskiBroj).ToList();

        if (_popis.Status == StatusPopisa.Zavrsen)
        {
            StavkeGrid.IsReadOnly = true;
            BtnSacuvaj.Visibility = Visibility.Collapsed;
            BtnZakljuci.Visibility = Visibility.Collapsed;
            TxtNaslov.Text += " (ZAKLJUČENO)";
        }
    }

    private void StavkeGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        // Osveži kalkulacije kolona koje nisu mapirane (ImaRazliku, Razlika)
        if (e.Row.Item is PopisnaStavka)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                StavkeGrid.Items.Refresh();
            }));
        }
    }

    private void BtnOtkazi_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void BtnSacuvaj_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _db.SaveChanges();
            MessageBox.Show("Stanje je uspešno sačuvano.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            StavkeGrid.Items.Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Greška pri čuvanju: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnZakljuci_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Da li ste sigurni da želite da zaključite popis? Nakon zaključivanja izmene više neće biti moguće.", "Zaključivanje popisa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            try
            {
                _popis.Status = StatusPopisa.Zavrsen;
                _db.SaveChanges();
                
                MessageBox.Show("Popis je uspešno zaključen.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greška: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
