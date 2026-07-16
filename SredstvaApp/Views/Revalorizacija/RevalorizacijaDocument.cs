using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SredstvaApp.Views.Revalorizacija;

public class RevalorizacijaDocument : IDocument
{
    private readonly List<RevalorizacijaResultViewModel> _stavke;
    private readonly string _nazivFirme;
    private readonly DateTime _od;
    private readonly DateTime _do;
    private readonly decimal _koeficijent;

    public RevalorizacijaDocument(
        List<RevalorizacijaResultViewModel> stavke,
        string nazivFirme,
        DateTime od,
        DateTime do_,
        decimal koeficijent)
    {
        _stavke = stavke;
        _nazivFirme = nazivFirme;
        _od = od;
        _do = do_;
        _koeficijent = koeficijent;
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
                col.Item().PaddingTop(6).Text("REVALORIZACIJA OSNOVNIH SREDSTAVA").Bold().FontSize(15);
                col.Item().Text($"Period: {_od:dd.MM.yyyy.} — {_do:dd.MM.yyyy.}  |  Koeficijent: {_koeficijent:F4}").FontSize(9).FontColor(Colors.Grey.Darken1);
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
                    header.Cell().Element(HdrStyle).Text("Inv. Br.").Bold();
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
                foreach (var r in _stavke.OrderBy(x => x.InventarskiBrojSort))
                {
                    bool imaEfekat = r.EfekatNabavna != 0;

                    table.Cell().Element(RowStyle).Text(r.InventarskiBroj);
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
            var ukNabavna = _stavke.Sum(r => r.StaraNabavna);
            var ukOtpisana = _stavke.Sum(r => r.StaraIspravka);
            var ukRevNab = _stavke.Sum(r => r.NovaNabavna);
            var ukRevIsp = _stavke.Sum(r => r.NovaIspravka);
            var ukEfekat = _stavke.Sum(r => r.EfekatNabavna);

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
