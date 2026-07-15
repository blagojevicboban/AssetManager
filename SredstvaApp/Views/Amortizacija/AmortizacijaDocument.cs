using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SredstvaApp.Views.Amortizacija;

public class AmortizacijaDocument : IDocument
{
    private readonly List<AmortizacijaResultViewModel> _stavke;
    private readonly string _nazivFirme;
    private readonly DateTime _od;
    private readonly DateTime _do;

    public AmortizacijaDocument(
        List<AmortizacijaResultViewModel> stavke,
        string nazivFirme,
        DateTime od,
        DateTime do_)
    {
        _stavke = stavke;
        _nazivFirme = nazivFirme;
        _od = od;
        _do = do_;
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
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(_nazivFirme).Bold().FontSize(12);
                col.Item().PaddingTop(6).Text("AMORTIZACIJA OSNOVNIH SREDSTAVA").Bold().FontSize(15);
                col.Item().Text($"Period: {_od:dd.MM.yyyy.} — {_do:dd.MM.yyyy.}").FontSize(9).FontColor(Colors.Grey.Darken1);
            });
            row.ConstantItem(160).AlignRight().Column(col =>
            {
                col.Item().Text($"Datum štampe: {DateTime.Now:dd.MM.yyyy.}").FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Broj sredstava: {_stavke.Count}").FontSize(8).FontColor(Colors.Grey.Darken1);
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
                    columns.ConstantColumn(70);  // Inv. Br.
                    columns.RelativeColumn();    // Naziv
                    columns.ConstantColumn(45);  // Stopa %
                    columns.ConstantColumn(90);  // Nabavna Vr.
                    columns.ConstantColumn(90);  // Prethodna isp.
                    columns.ConstantColumn(90);  // Nova Amortizacija
                    columns.ConstantColumn(90);  // Nova Ispravka ukupno
                    columns.ConstantColumn(90);  // Sadašnja vrednost
                });

                // Header tabele
                table.Header(header =>
                {
                    header.Cell().Element(HdrStyle).Text("Inv. Br.").Bold();
                    header.Cell().Element(HdrStyle).Text("Naziv sredstva").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Stopa %").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Nabavna Vr.").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Preth. isp.").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Nova amort.").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Isp. ukupno").Bold();
                    header.Cell().Element(HdrStyle).AlignRight().Text("Sadašnja Vr.").Bold();

                    static IContainer HdrStyle(IContainer c)
                        => c.Background(Colors.Indigo.Darken4)
                            .PaddingVertical(4).PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8.5f));
                });

                // Redovi
                foreach (var r in _stavke)
                {
                    bool imaAmort = r.NovaAmortizacija > 0;

                    table.Cell().Element(RowStyle).Text(r.InventarskiBroj);
                    table.Cell().Element(RowStyle).Text(r.Naziv);
                    table.Cell().Element(RowStyle).AlignRight().Text(r.StopaAmortizacije.ToString("N2"));
                    table.Cell().Element(RowStyle).AlignRight().Text(r.NabavnaVrednost.ToString("N2"));
                    table.Cell().Element(RowStyle).AlignRight().Text(r.PrethodnaIspravka.ToString("N2"));

                    // Nova amortizacija — narandžasta ako > 0
                    table.Cell().Element(RowStyle).AlignRight().Text(t =>
                    {
                        var span = t.Span(r.NovaAmortizacija.ToString("N2"))
                             .FontColor(imaAmort ? Colors.Orange.Darken2 : Colors.Grey.Darken1);
                        if (imaAmort) span.Bold();
                    });

                    table.Cell().Element(RowStyle).AlignRight().Text(r.NovaIspravkaUkupno.ToString("N2"));

                    // Sadašnja vrednost — zelena ako > 0
                    table.Cell().Element(RowStyle).AlignRight().Text(t =>
                    {
                        var sd = r.SadasnjaVrednost;
                        var span = t.Span(sd.ToString("N2"))
                             .FontColor(sd > 0 ? Colors.Green.Darken2 : Colors.Grey.Darken1);
                        if (sd > 0) span.Bold();
                    });

                    static IContainer RowStyle(IContainer c)
                        => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                            .PaddingVertical(3).PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.FontSize(8.5f));
                }
            });

            // Ukupni zbir (kao "UKUPNI ZBIR" u Clipper-u)
            var ukNabavna = _stavke.Sum(r => r.NabavnaVrednost);
            var ukPrethodna = _stavke.Sum(r => r.PrethodnaIspravka);
            var ukNova = _stavke.Sum(r => r.NovaAmortizacija);
            var ukUkupno = _stavke.Sum(r => r.NovaIspravkaUkupno);
            var ukSadasnja = _stavke.Sum(r => r.SadasnjaVrednost);

            col.Item().PaddingTop(4).Table(sumTable =>
            {
                sumTable.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(70);
                    c.RelativeColumn();
                    c.ConstantColumn(45);
                    c.ConstantColumn(90);
                    c.ConstantColumn(90);
                    c.ConstantColumn(90);
                    c.ConstantColumn(90);
                    c.ConstantColumn(90);
                });

                sumTable.Cell().Element(SumStyle).Text("UKUPNO").Bold();
                sumTable.Cell().Element(SumStyle).Text("");
                sumTable.Cell().Element(SumStyle).Text("");
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukNabavna.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukPrethodna.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(t =>
                    t.Span(ukNova.ToString("N2")).Bold().FontColor(Colors.Orange.Darken2));
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukUkupno.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(t =>
                    t.Span(ukSadasnja.ToString("N2")).Bold().FontColor(Colors.Green.Darken2));

                static IContainer SumStyle(IContainer c)
                    => c.Background(Colors.Indigo.Lighten5)
                        .BorderTop(1).BorderColor(Colors.Indigo.Darken3)
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
