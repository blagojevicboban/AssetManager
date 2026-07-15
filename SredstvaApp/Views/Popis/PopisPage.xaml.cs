using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Popis;

public partial class PopisPage : Page
{
    private readonly SredstvaDbContext _db;

    public PopisPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += PopisPage_Loaded;
    }

    private void PopisPage_Loaded(object sender, RoutedEventArgs e)
    {
        LoadGodine();
        LoadKomisije();
        LoadPopisi();
    }

    private void LoadGodine()
    {
        var godine = _db.Popisi.Select(p => p.Godina).Distinct().OrderByDescending(g => g).ToList();
        if (!godine.Contains(DateTime.Now.Year))
            godine.Insert(0, DateTime.Now.Year);

        CmbGodina.ItemsSource = godine;
        if (CmbGodina.SelectedItem == null)
            CmbGodina.SelectedItem = DateTime.Now.Year;
    }

    private void LoadKomisije()
    {
        var komisije = _db.Komisije.OrderByDescending(k => k.DatumKreiranja).ToList();
        KomisijeGrid.ItemsSource = komisije;
    }

    private void LoadPopisi()
    {
        if (CmbGodina.SelectedItem == null) return;
        int godina = (int)CmbGodina.SelectedItem;

        var popisi = _db.Popisi
            .Include(p => p.Komisija)
            .Where(p => p.Godina == godina)
            .OrderByDescending(p => p.DatumPopisa)
            .ToList();

        PopisGrid.ItemsSource = popisi;
    }

    private void CmbGodina_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadPopisi();
    }

    private void BtnNovaKomisija_Click(object sender, RoutedEventArgs e)
    {
        // Pojednostavljen unos komisije, u produkciji bi bio poseban prozor
        var naziv = "Popisna komisija " + DateTime.Now.Year;
        var komisija = new SredstvaData.Models.Komisija
        {
            Naziv = naziv,
            DatumKreiranja = DateTime.Now,
            JeAktivna = true
        };
        _db.Komisije.Add(komisija);
        _db.SaveChanges();
        
        LoadKomisije();
        MessageBox.Show($"Kreirana komisija: {naziv}", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void BtnNoviPopis_Click(object sender, RoutedEventArgs e)
    {
        var aktivnaKomisija = _db.Komisije.FirstOrDefault(k => k.JeAktivna);
        if (aktivnaKomisija == null)
        {
            MessageBox.Show("Nema aktivne komisije. Prvo kreirajte komisiju u tabu 'Komisije'.", "Upozorenje", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show("Da li želite da generišete novi popis za tekuću godinu? Ovo će evidentirati sva aktivna osnovna sredstva na popisnu listu.", "Novi popis", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            using var transaction = _db.Database.BeginTransaction();
            try
            {
                var popis = new SredstvaData.Models.Popis
                {
                    DatumPopisa = DateTime.Now,
                    Godina = DateTime.Now.Year,
                    KomisijaId = aktivnaKomisija.Id,
                    Status = StatusPopisa.UToku
                };
                _db.Popisi.Add(popis);
                _db.SaveChanges(); // to get Id

                var aktivnaSredstva = _db.Sredstva.Where(s => s.JeAktivno).ToList();
                foreach (var sredstvo in aktivnaSredstva)
                {
                    var stavka = new PopisnaStavka
                    {
                        PopisId = popis.Id,
                        SredstvoId = sredstvo.Id,
                        KnjiznaKolicina = sredstvo.Kolicina,
                        KnjiznaVrednost = sredstvo.NabavnaVrednost - sredstvo.IspravkaVrednosti,
                        PopisanaKolicina = sredstvo.Kolicina, // Podrazumevano je isto
                        ProcenjenaVrednost = sredstvo.NabavnaVrednost - sredstvo.IspravkaVrednosti // Podrazumevano je isto
                    };
                    _db.PopisneStavke.Add(stavka);
                }

                _db.SaveChanges();
                transaction.Commit();

                LoadPopisi();
                MessageBox.Show("Nova popisna lista je uspesno generisana.", "Uspeh", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                MessageBox.Show("Greška: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void PopisGrid_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (PopisGrid.SelectedItem is SredstvaData.Models.Popis popis)
        {
            var window = new UpisPopisaWindow(popis.Id, _db);
            window.ShowDialog();
            LoadPopisi(); // Osvježi nakon povratka
        }
    }
}
