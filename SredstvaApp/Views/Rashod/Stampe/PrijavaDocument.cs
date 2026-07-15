using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SredstvaApp.Views.Rashod.Stampe;

public class PrijavaDocument : IDocument
{
    private readonly int _brojNaloga;
    private readonly DateTime _datumAktiviranja;
    private readonly string _dobavljac;
    private readonly IEnumerable<PrijavaStavkaViewModel> _stavke;
    private readonly string _nazivFirme;

    public PrijavaDocument(
        int brojNaloga, 
        DateTime datumAktiviranja, 
        string dobavljac, 
        IEnumerable<PrijavaStavkaViewModel> stavke, 
        string nazivFirme)
    {
        _brojNaloga = brojNaloga;
        _datumAktiviranja = datumAktiviranja;
        _dobavljac = dobavljac;
        _stavke = stavke;
        _nazivFirme = nazivFirme;
    }

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

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
                col.Item().PaddingTop(10).Text($"PRIJAVA (NALOG ZA KNJIŽENJE) BR: {_brojNaloga}").Bold().FontSize(16);
                col.Item().Text($"Datum prijave: {_datumAktiviranja:dd.MM.yyyy.}");
                col.Item().Text($"Dobavljač/Partner: {_dobavljac}");
            });

            row.ConstantItem(150).AlignRight().Column(col =>
            {
                col.Item().Text($"Datum štampe: {DateTime.Now:dd.MM.yyyy.}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(col =>
        {
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // Rbr
                    columns.ConstantColumn(60);  // Inv. Broj
                    columns.RelativeColumn();    // Naziv
                    columns.ConstantColumn(50);  // Grupa
                    columns.ConstantColumn(50);  // Stopa
                    columns.ConstantColumn(50);  // Konto
                    columns.ConstantColumn(30);  // OJ
                    columns.ConstantColumn(80);  // Nabavna vrednost
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Rbr").Bold();
                    header.Cell().Element(CellStyle).Text("Inv. Broj").Bold();
                    header.Cell().Element(CellStyle).Text("Naziv osnovnog sredstva").Bold();
                    header.Cell().Element(CellStyle).Text("Am. Gr.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Stopa %").Bold();
                    header.Cell().Element(CellStyle).Text("Konto").Bold();
                    header.Cell().Element(CellStyle).Text("OJ").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Nabavna vr.").Bold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold())
                                        .PaddingVertical(5)
                                        .PaddingRight(5)
                                        .BorderBottom(1)
                                        .BorderColor(Colors.Black);
                    }
                });

                foreach (var stavka in _stavke)
                {
                    table.Cell().Element(CellStyle).Text(stavka.RedBroj.ToString());
                    table.Cell().Element(CellStyle).Text(stavka.InventarskiBroj);
                    table.Cell().Element(CellStyle).Text(stavka.Naziv);
                    table.Cell().Element(CellStyle).Text(stavka.AmortizacionaGrupa);
                    table.Cell().Element(CellStyle).AlignRight().Text(stavka.StopaAmortizacije.ToString("N2"));
                    table.Cell().Element(CellStyle).Text(stavka.Konto);
                    table.Cell().Element(CellStyle).Text(stavka.ObracunskaJedinica.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(stavka.NabavnaVrednost.ToString("N2"));

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(5)
                                        .PaddingRight(5);
                    }
                }
            });

            var ukupno = _stavke.Sum(x => x.NabavnaVrednost);
            col.Item().PaddingTop(10).AlignRight().Text($"Ukupna nabavna vrednost: {ukupno:N2}").Bold().FontSize(12);
            
            col.Item().PaddingTop(50).Row(row =>
            {
                row.RelativeItem().AlignCenter().Text("Sastavio: ___________________");
                row.RelativeItem().AlignCenter().Text("Odobrio: ___________________");
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(x =>
        {
            x.Span("Strana ");
            x.CurrentPageNumber();
            x.Span(" od ");
            x.TotalPages();
        });
    }
}
