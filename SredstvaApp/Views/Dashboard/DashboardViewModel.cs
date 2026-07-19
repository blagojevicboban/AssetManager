using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.Measure;
using SkiaSharp;
using SredstvaData;
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Collections.Generic;

namespace SredstvaApp.Views.Dashboard;

public partial class DashboardViewModel : ObservableObject
{
    private readonly SredstvaDbContext _db;

    [ObservableProperty]
    private int _ukupanBrojSredstava;

    [ObservableProperty]
    private decimal _ukupnaNabavnaVrednost;

    [ObservableProperty]
    private decimal _ukupnaSadasnjaVrednost;

    [ObservableProperty]
    private ISeries[] _statusSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _kontoSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _topSredstvaSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _topSredstvaXAxes = Array.Empty<Axis>();

    public DashboardViewModel(SredstvaDbContext db)
    {
        _db = db;
        UcitajPodatke();
    }

    public void UcitajPodatke()
    {
        var svaSredstva = _db.Sredstva.ToList();

        UkupanBrojSredstava = svaSredstva.Count;
        UkupnaNabavnaVrednost = svaSredstva.Sum(s => s.NabavnaVrednost);
        UkupnaSadasnjaVrednost = svaSredstva.Sum(s => s.SadasnjaVrednost);

        // Status Sredstava (Donut)
        var aktivna = svaSredstva.Count(s => s.JeAktivno);
        var neaktivna = svaSredstva.Count(s => !s.JeAktivno);

        StatusSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[] { aktivna }, Name = "Aktivna", InnerRadius = 30 },
            new PieSeries<int> { Values = new[] { neaktivna }, Name = "Rashodovana", InnerRadius = 30 }
        };

        // Vrednost po Kontima (Pie)
        var poKontima = svaSredstva
            .Where(s => s.JeAktivno && s.SadasnjaVrednost > 0)
            .GroupBy(s => string.IsNullOrWhiteSpace(s.Konto) ? "Nepoznato" : s.Konto)
            .Select(g => new { Konto = g.Key, Vrednost = (double)g.Sum(s => s.SadasnjaVrednost) })
            .OrderByDescending(x => x.Vrednost)
            .Take(10) // Top 10 konta
            .ToList();

        var kontoPieSeries = new List<ISeries>();
        foreach (var k in poKontima)
        {
            kontoPieSeries.Add(new PieSeries<double>
            {
                Values = new[] { k.Vrednost },
                Name = $"Konto: {k.Konto}",
                DataLabelsPosition = PolarLabelsPosition.Outer,
                DataLabelsFormatter = point => $"{point.Context.Series.Name}: {point.Model:N0}",
                ToolTipLabelFormatter = point => $"{point.Model:N2}"
            });
        }
        KontoSeries = kontoPieSeries.ToArray();

        // Top 5 Najvrednijih Sredstava (Bar Chart)
        var top5 = svaSredstva
            .Where(s => s.JeAktivno)
            .OrderByDescending(s => s.SadasnjaVrednost)
            .Take(5)
            .ToList();

        TopSredstvaSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = top5.Select(s => (double)s.SadasnjaVrednost).ToArray(),
                Name = "Sadašnja vrednost",
                Fill = new SolidColorPaint(SKColor.Parse("#2B4B80")), // Primary color
                DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#333333")),
                DataLabelsPosition = DataLabelsPosition.Top,
                DataLabelsFormatter = point => point.Model.ToString("N0"),
                YToolTipLabelFormatter = point => $"{point.Model:N2}"
            }
        };

        TopSredstvaXAxes = new Axis[]
        {
            new Axis
            {
                Labels = top5.Select(s => string.IsNullOrWhiteSpace(s.Naziv) ? s.InventarskiBroj : $"({s.InventarskiBroj}) {s.Naziv}").ToArray(),
                LabelsRotation = 15,
                TextSize = 12
            }
        };
    }
}
