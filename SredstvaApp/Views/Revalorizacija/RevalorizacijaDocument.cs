using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SredstvaApp.Views.Revalorizacija;

public class RevalorizacijaDocument : IDocument
{
    private readonly List<RevalorizacijaResultViewModel> _rezultati;
    private readonly SredstvaData.Models.Firma? _firma;
    private readonly DateTime _odDatuma;
    private readonly DateTime _doDatuma;
    private readonly decimal _godKoeficijent;
    private readonly string _primaryColor = "#2B4B80";

    public RevalorizacijaDocument(List<RevalorizacijaResultViewModel> rezultati, SredstvaData.Models.Firma? firma, DateTime odDatuma, DateTime doDatuma, decimal godKoeficijent)
    {
        _rezultati = rezultati;
        _firma = firma;
        _odDatuma = odDatuma;
        _doDatuma = doDatuma;
        _godKoeficijent = godKoeficijent;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(1, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Calibri"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"REVALORIZACIJA OSNOVNIH SREDSTAVA").FontSize(16).SemiBold().FontColor(_primaryColor);
                column.Item().Text($"Za period: {_odDatuma:dd.MM.yyyy} - {_doDatuma:dd.MM.yyyy}").FontSize(12).FontColor(Colors.Grey.Darken2);
                column.Item().Text($"God. koeficijent: {_godKoeficijent:N3}").FontSize(10).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(250).AlignRight().Column(column =>
            {
                if (_firma != null)
                {
                    column.Item().AlignRight().Text(_firma.Naziv).FontSize(12).SemiBold().FontColor(Colors.Black);
                    if (!string.IsNullOrEmpty(_firma.Mesto))
                        column.Item().AlignRight().Text(_firma.Mesto).FontSize(10).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrEmpty(_firma.PIB))
                        column.Item().AlignRight().Text($"PIB: {_firma.PIB}").FontSize(10).FontColor(Colors.Grey.Darken2);
                }
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(6).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(70);   // Inv. Br.
                    columns.RelativeColumn();     // Naziv
                    columns.ConstantColumn(90);   // Stara Nabavna
                    columns.ConstantColumn(90);   // Stara Ispravka
                    columns.ConstantColumn(50);   // Koef.
                    columns.ConstantColumn(90);   // Rev. Nabavne (NovaNabavna)
                    columns.ConstantColumn(90);   // Rev. Ispravke (NovaIspravka)
                    columns.ConstantColumn(90);   // Efekat (EfekatNabavna)
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(HdrStyle).Text("Šifra").Bold();
                    header.Cell().Element(HdrStyle).Text("Naziv sredstva").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Nabavna Vr.").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Otpisana Vr.").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Koef.").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Rev. Nabavne").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Rev. Ispravke").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Efekat").Bold();

                    static IContainer HdrStyle(IContainer c)
                        => c.Background(Colors.DeepPurple.Darken4)
                            .PaddingVertical(4).PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8.5f));
                });

                // Redovi
                foreach (var r in _rezultati.OrderBy(x => x.LegacySifra))
                {
                    bool imaEfekat = r.EfekatNabavna != 0;

                    table.Cell().Element(RowStyle).Text(r.LegacySifra.ToString());
                    table.Cell().Element(RowStyle).Text(r.Naziv);
                    table.Cell().Element(RowStyle).AlignRight().Text(r.StaraNabavna.ToString("N2"));
                    table.Cell().Element(RowStyle).AlignRight().Text(r.StaraIspravka.ToString("N2"));
                    table.Cell().Element(RowStyle).AlignRight().Text(r.PrimenjeniGodisnjiKoef.ToString("F4"));

                    table.Cell().Element(RowStyle).AlignRight().Text(t =>
                    {
                        var span = t.Span(r.NovaNabavna.ToString("N2"))
                            .FontColor(imaEfekat ? Colors.Orange.Darken2 : Colors.Grey.Darken1);
                        if (imaEfekat) span.Bold();
                    });

                    table.Cell().Element(RowStyle).AlignRight().Text(t =>
                    {
                        var span = t.Span(r.NovaIspravka.ToString("N2"))
                            .FontColor(imaEfekat ? Colors.Orange.Darken3 : Colors.Grey.Darken1);
                        if (imaEfekat) span.Bold();
                    });

                    table.Cell().Element(RowStyle).AlignRight().Text(t =>
                    {
                        var ef = r.EfekatNabavna;
                        var span = t.Span(ef.ToString("N2"))
                            .FontColor(ef > 0 ? Colors.Green.Darken2 : ef < 0 ? Colors.Red.Darken2 : Colors.Grey.Darken1);
                        if (ef != 0) span.Bold();
                    });

                    static IContainer RowStyle(IContainer c)
                        => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(3).PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.FontSize(8.5f));
                }
            });

            // UKUPNI ZBIR (kao u Clipper-u)
            var ukNabavna = _rezultati.Sum(r => r.StaraNabavna);
            var ukOtpisana = _rezultati.Sum(r => r.StaraIspravka);
            var ukRevNab = _rezultati.Sum(r => r.NovaNabavna);
            var ukRevIsp = _rezultati.Sum(r => r.NovaIspravka);
            var ukEfekat = _rezultati.Sum(r => r.EfekatNabavna);

            col.Item().PaddingTop(4).Table(sumTable =>
            {
                sumTable.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(70);
                    c.RelativeColumn();
                    c.ConstantColumn(90);
                    c.ConstantColumn(90);
                    c.ConstantColumn(50);
                    c.ConstantColumn(90);
                    c.ConstantColumn(90);
                    c.ConstantColumn(90);
                });

                sumTable.Cell().Element(SumStyle).Text("UKUPNO").Bold();
                sumTable.Cell().Element(SumStyle).Text("");
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukNabavna.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukOtpisana.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).Text("");
                sumTable.Cell().Element(SumStyle).AlignRight().Text(t =>
                    t.Span(ukRevNab.ToString("N2")).Bold().FontColor(Colors.Orange.Darken2));
                sumTable.Cell().Element(SumStyle).AlignRight().Text(t =>
                    t.Span(ukRevIsp.ToString("N2")).Bold().FontColor(Colors.Orange.Darken3));
                sumTable.Cell().Element(SumStyle).AlignRight().Text(t =>
                    t.Span(ukEfekat.ToString("N2")).Bold()
                     .FontColor(ukEfekat >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2));

                static IContainer SumStyle(IContainer c)
                    => c.Background(Colors.DeepPurple.Lighten5)
                        .BorderTop(1).BorderColor(Colors.DeepPurple.Darken3)
                        .PaddingVertical(4).PaddingHorizontal(4)
                        .DefaultTextStyle(x => x.FontSize(9f));
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Strana ").FontSize(7).FontColor(Colors.Grey.Darken1);
            x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Darken1);
            x.Span(" od ").FontSize(7).FontColor(Colors.Grey.Darken1);
            x.TotalPages().FontSize(7).FontColor(Colors.Grey.Darken1);
        });
    }
}
