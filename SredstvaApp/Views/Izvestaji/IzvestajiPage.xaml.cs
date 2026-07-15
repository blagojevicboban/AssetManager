using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SredstvaData;
using SredstvaData.Models;

namespace SredstvaApp.Views.Izvestaji;

// ── View modeli za izveštaje ─────────────────────────────────────────────────

public class PopisRedViewModel
{
    public string InventarskiBroj { get; init; } = string.Empty;
    public string Naziv { get; init; } = string.Empty;
    public string AmortizacionaGrupa { get; init; } = string.Empty;
    public decimal StopaAmortizacije { get; init; }
    public DateTime DatumAktiviranja { get; init; }
    public string DatumAktiviranjaStr => DatumAktiviranja == DateTime.MinValue ? "—" : DatumAktiviranja.ToString("dd.MM.yyyy");
    public decimal NabavnaVrednost { get; init; }
    public decimal IspravkaVrednosti { get; init; }
    public decimal SadasnjaVrednost { get; init; }
}

public class RekapRedViewModel
{
    public string Grupacija { get; init; } = string.Empty;
    public int BrojSredstava { get; init; }
    public decimal NabavnaVrednost { get; init; }
    public decimal IspravkaVrednosti { get; init; }
    public decimal SadasnjaVrednost { get; init; }
    public decimal ProcenatOtpisa => NabavnaVrednost == 0 ? 0 : Math.Round(IspravkaVrednosti / NabavnaVrednost * 100, 1);
}

// ── Page ─────────────────────────────────────────────────────────────────────

public partial class IzvestajiPage : Page
{
    private readonly SredstvaDbContext _db;
    private List<Sredstvo> _sredstva = new();
    private string _currentReport = string.Empty;
    private IEnumerable<object> _currentData = Enumerable.Empty<object>();

    public IzvestajiPage(SredstvaDbContext db)
    {
        InitializeComponent();
        _db = db;
        Loaded += IzvestajiPage_Loaded;
    }

    private void IzvestajiPage_Loaded(object sender, RoutedEventArgs e)
    {
        _sredstva = _db.Sredstva.ToList();
    }

    // ── Popis ─────────────────────────────────────────────────────────────────

    private void BtnPopis_Click(object sender, RoutedEventArgs e)
    {
        IzvestajNaslov.Text = "📋  Popis svih sredstava";
        _currentReport = "popis";

        var data = _sredstva
            .OrderBy(s => s.InventarskiBroj)
            .Select(s => new PopisRedViewModel
            {
                InventarskiBroj = s.InventarskiBroj,
                Naziv = s.Naziv,
                AmortizacionaGrupa = s.AmortizacionaGrupa,
                StopaAmortizacije = s.StopaAmortizacije,
                DatumAktiviranja = s.DatumAktiviranja,
                NabavnaVrednost = s.NabavnaVrednost,
                IspravkaVrednosti = s.IspravkaVrednosti,
                SadasnjaVrednost = s.SadasnjaVrednost
            }).ToList();

        SetupPopisColumns();
        IzvestajGrid.ItemsSource = data;
        _currentData = data;
        ShowGrid(data.Sum(r => r.NabavnaVrednost), data.Sum(r => r.SadasnjaVrednost),
            $"Prikazano {data.Count} sredstava");
    }

    // ── Rekapitulacije ────────────────────────────────────────────────────────

    private void BtnRekap_Click(object sender, RoutedEventArgs e)
    {
        var tag = (sender as Button)?.Tag?.ToString() ?? "konto";
        IEnumerable<IGrouping<string, Sredstvo>> grupe;
        string naslov;

        switch (tag)
        {
            case "oj":
                naslov = "🏢  Rekapitulacija po obračunskoj jedinici";
                grupe = _sredstva.GroupBy(s => s.FirmaId.ToString());
                break;
            case "amgrupa":
                naslov = "📈  Rekapitulacija po amortizacionoj grupi";
                grupe = _sredstva.GroupBy(s => s.AmortizacionaGrupa);
                break;
            default: // konto
                naslov = "📊  Rekapitulacija po amortizacionoj grupi";
                grupe = _sredstva.GroupBy(s => s.AmortizacionaGrupa);
                break;
        }

        IzvestajNaslov.Text = naslov;
        _currentReport = "rekap_" + tag;

        var data = grupe
            .OrderBy(g => g.Key)
            .Select(g => new RekapRedViewModel
            {
                Grupacija = string.IsNullOrWhiteSpace(g.Key) || g.Key == "0" ? "(nije definisano)" : g.Key,
                BrojSredstava = g.Count(),
                NabavnaVrednost = g.Sum(s => s.NabavnaVrednost),
                IspravkaVrednosti = g.Sum(s => s.IspravkaVrednosti),
                SadasnjaVrednost = g.Sum(s => s.SadasnjaVrednost)
            }).ToList();

        SetupRekapColumns(tag);
        IzvestajGrid.ItemsSource = data;
        _currentData = data;
        ShowGrid(data.Sum(r => r.NabavnaVrednost), data.Sum(r => r.SadasnjaVrednost),
            $"Prikazano {data.Count} grupacija  •  Ukupno {data.Sum(r => r.BrojSredstava)} sredstava");
    }

    // ── Kolone ────────────────────────────────────────────────────────────────

    private void SetupPopisColumns()
    {
        IzvestajGrid.Columns.Clear();
        IzvestajGrid.Columns.Add(TextCol("Inv. Br.", "InventarskiBroj", 90));
        IzvestajGrid.Columns.Add(TextCol("Naziv Sredstva", "Naziv", null, minWidth: 180));
        IzvestajGrid.Columns.Add(TextCol("Am. Gr.", "AmortizacionaGrupa", 70, center: true));
        IzvestajGrid.Columns.Add(TextCol("Stopa %", "StopaAmortizacije", 75, format: "N2", right: true));
        IzvestajGrid.Columns.Add(TextCol("Datum Akt.", "DatumAktiviranjaStr", 110, center: true));
        IzvestajGrid.Columns.Add(MoneyCol("Nabavna Vr.", "NabavnaVrednost", 130));
        IzvestajGrid.Columns.Add(MoneyCol("Ispravka Vr.", "IspravkaVrednosti", 130, warning: true));
        IzvestajGrid.Columns.Add(MoneyCol("Sadašnja Vr.", "SadasnjaVrednost", 130, accent: true));
    }

    private void SetupRekapColumns(string tag)
    {
        IzvestajGrid.Columns.Clear();
        var grupLabel = tag switch { "oj" => "Obr. Jedinica", "amgrupa" => "Am. Grupa", _ => "Am. Grupa" };
        IzvestajGrid.Columns.Add(TextCol(grupLabel, "Grupacija", null, minWidth: 140));
        IzvestajGrid.Columns.Add(TextCol("Br. Sred.", "BrojSredstava", 90, center: true));
        IzvestajGrid.Columns.Add(MoneyCol("Nabavna Vr.", "NabavnaVrednost", 150));
        IzvestajGrid.Columns.Add(MoneyCol("Ispravka Vr.", "IspravkaVrednosti", 150, warning: true));
        IzvestajGrid.Columns.Add(MoneyCol("Sadašnja Vr.", "SadasnjaVrednost", 150, accent: true));
        IzvestajGrid.Columns.Add(TextCol("% Otpisa", "ProcenatOtpisa", 90, format: "N1", right: true));
    }

    // ── Helpers za kolone ─────────────────────────────────────────────────────

    private static DataGridTextColumn TextCol(string header, string binding,
        double? width, string? format = null, bool center = false,
        bool right = false, double minWidth = 0)
    {
        var col = new DataGridTextColumn
        {
            Header = header,
            Binding = format != null
                ? new System.Windows.Data.Binding(binding) { StringFormat = format }
                : new System.Windows.Data.Binding(binding),
            Width = width.HasValue ? new DataGridLength(width.Value) : new DataGridLength(1, DataGridLengthUnitType.Star),
            MinWidth = minWidth
        };
        if (center || right)
        {
            col.ElementStyle = new Style(typeof(TextBlock))
            {
                Setters = {
                    new Setter(TextBlock.HorizontalAlignmentProperty,
                        center ? HorizontalAlignment.Center : HorizontalAlignment.Right),
                    new Setter(TextBlock.PaddingProperty, new Thickness(0,0,right?8:0,0))
                }
            };
        }
        return col;
    }

    private static DataGridTextColumn MoneyCol(string header, string binding, double width,
        bool warning = false, bool accent = false)
    {
        var color = accent
            ? System.Windows.Media.Color.FromRgb(0x2D, 0x6A, 0x4F)
            : warning
                ? System.Windows.Media.Color.FromRgb(0xF5, 0x9E, 0x0B)
                : System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x2E);

        var col = new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding(binding) { StringFormat = "N2" },
            Width = new DataGridLength(width),
            ElementStyle = new Style(typeof(TextBlock))
            {
                Setters = {
                    new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right),
                    new Setter(TextBlock.PaddingProperty, new Thickness(0,0,10,0)),
                    new Setter(TextBlock.FontFamilyProperty, new System.Windows.Media.FontFamily("Consolas")),
                    new Setter(TextBlock.ForegroundProperty,
                        new System.Windows.Media.SolidColorBrush(color)),
                    new Setter(TextBlock.FontWeightProperty, accent ? FontWeights.SemiBold : FontWeights.Normal)
                }
            }
        };
        return col;
    }

    // ── UI helpers ────────────────────────────────────────────────────────────

    private void ShowGrid(decimal ukupnoNab, decimal ukupnoSad, string zbir)
    {
        PlaceholderPanel.Visibility = Visibility.Collapsed;
        IzvestajGrid.Visibility = Visibility.Visible;
        BtnExport.IsEnabled = true;
        UkupnoNabavna.Text = ukupnoNab.ToString("N2");
        UkupnoSadasnja.Text = ukupnoSad.ToString("N2");
        ZbirText.Text = zbir;
    }

    // ── Export CSV ────────────────────────────────────────────────────────────

    private void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Sačuvaj izveštaj",
            Filter = "CSV fajl (*.csv)|*.csv",
            FileName = $"izvestaj_{_currentReport}_{DateTime.Now:yyyyMMdd}"
        };

        if (dlg.ShowDialog() != true) return;

        try
        {
            var sb = new StringBuilder();

            // Header
            var headers = IzvestajGrid.Columns.Select(c => c.Header?.ToString() ?? "");
            sb.AppendLine(string.Join(";", headers));

            // Redovi
            foreach (var item in _currentData)
            {
                var values = IzvestajGrid.Columns.Select(col =>
                {
                    if (col is DataGridTextColumn tc && tc.Binding is System.Windows.Data.Binding b)
                    {
                        var prop = item.GetType().GetProperty(b.Path.Path);
                        return prop?.GetValue(item)?.ToString()?.Replace(";", ",") ?? "";
                    }
                    return "";
                });
                sb.AppendLine(string.Join(";", values));
            }

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show($"Izveštaj sačuvan:\n{dlg.FileName}", "Export uspešan",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Greška pri eksportu: {ex.Message}", "Greška",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
