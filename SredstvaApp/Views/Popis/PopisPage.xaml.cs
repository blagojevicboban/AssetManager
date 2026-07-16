using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using SredstvaData;
using SredstvaData.Models;
using QuestPDF.Fluent;

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
        SyncSredstvaSaKarticama(_db);
        LoadGodine();
        LoadKomisije();
        LoadPopisi();
    }

    private void SyncSredstvaSaKarticama(SredstvaDbContext db)
    {
        var sredstva = db.Sredstva.Where(s => s.ObracunskaJedinica == 0 || s.Konto == "").ToList();
        if (sredstva.Count == 0) return;

        foreach (var s in sredstva)
        {
            var kartice = db.Kartice.Where(k => k.SredstvoId == s.Id).ToList();
            if (kartice.Count > 0)
            {
                var lastKartica = kartice.OrderByDescending(k => k.Datum).ThenByDescending(k => k.Id).First();
                s.Konto = lastKartica.Konto ?? "";
                if (lastKartica.ObracunskaJedinica > 0)
                {
                    s.ObracunskaJedinica = lastKartica.ObracunskaJedinica;
                }
                
                s.NabavnaVrednost = kartice.Sum(k => k.NabavnaVrednost);
                s.IspravkaVrednosti = kartice.Sum(k => k.IspravkaVrednosti);
                s.SadasnjaVrednost = s.NabavnaVrednost - s.IspravkaVrednosti;
            }
        }
        db.SaveChanges();
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

    private void PopisGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = PopisGrid.SelectedItem != null;
        if (BtnStampaPrazne != null) BtnStampaPrazne.IsEnabled = hasSelection;
        if (BtnStampaIzvestaj != null) BtnStampaIzvestaj.IsEnabled = hasSelection;
    }

    private void BtnStampaPrazne_Click(object sender, RoutedEventArgs e)
    {
        if (PopisGrid.SelectedItem is SredstvaData.Models.Popis popis)
        {
            var stavke = _db.PopisneStavke
                .Include(s => s.Sredstvo)
                .Where(s => s.PopisId == popis.Id)
                .OrderBy(s => s.Sredstvo.ObracunskaJedinica)
                .ThenBy(s => s.Sredstvo.Konto)
                .ToList();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"PraznaPopisnaLista_{popis.Godina}.pdf",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var firma = _db.Firme.FirstOrDefault();
                    var document = new PraznaPopisnaListaDocument(popis, stavke, firma);
                    document.GeneratePdf(dialog.FileName);
                    
                    if (MessageBox.Show("PDF je uspešno generisan. Da li želite da ga otvorite?", "Uspeh", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška prilikom generisanja PDF-a: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void BtnStampaIzvestaj_Click(object sender, RoutedEventArgs e)
    {
        if (PopisGrid.SelectedItem is SredstvaData.Models.Popis popis)
        {
            var stavke = _db.PopisneStavke
                .Include(s => s.Sredstvo)
                .Where(s => s.PopisId == popis.Id)
                .OrderBy(s => s.Sredstvo.ObracunskaJedinica)
                .ThenBy(s => s.Sredstvo.Konto)
                .ToList();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"IzvestajOPopisu_{popis.Godina}.pdf",
                DefaultExt = ".pdf",
                Filter = "PDF documents (.pdf)|*.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var firma = _db.Firme.FirstOrDefault();
                    var document = new PopisIzvestajDocument(popis, stavke, firma);
                    document.GeneratePdf(dialog.FileName);
                    
                    if (MessageBox.Show("PDF je uspešno generisan. Da li želite da ga otvorite?", "Uspeh", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = dialog.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška prilikom generisanja PDF-a: " + ex.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
