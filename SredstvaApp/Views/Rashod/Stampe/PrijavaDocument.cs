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
    private readonly SredstvaData.Models.Firma? _firma;
    private readonly string _primaryColor = "#2B4B80";

    public PrijavaDocument(
        int brojNaloga, 
        DateTime datumAktiviranja, 
        string dobavljac, 
        IEnumerable<PrijavaStavkaViewModel> stavke, 
        SredstvaData.Models.Firma? firma)
    {
        _brojNaloga = brojNaloga;
        _datumAktiviranja = datumAktiviranja;
        _dobavljac = dobavljac;
        _stavke = stavke;
        _firma = firma;
    }

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4.Portrait());
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
                col.Item().PaddingTop(10).Text($"PRIJAVA (NALOG ZA KNJIŽENJE) BR: {_brojNaloga}").SemiBold().FontSize(16).FontColor(_primaryColor);
                col.Item().Text($"Datum prijave: {_datumAktiviranja:dd.MM.yyyy.}").FontSize(10).FontColor(Colors.Grey.Medium);
                col.Item().Text($"Dobavljač/Partner: {_dobavljac}").FontSize(10).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(250).AlignRight().Column(column =>
            {
                if (_firma != null)
                {
                    column.Item().AlignRight().Text(_firma.Naziv).FontSize(12).SemiBold().FontColor(Colors.Black);
                    if (!string.IsNullOrEmpty(_firma.Adresa) || !string.IsNullOrEmpty(_firma.Mesto))
                        column.Item().AlignRight().Text($"{_firma.Adresa}, {_firma.Mesto}".Trim(',', ' ')).FontSize(10).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrEmpty(_firma.PIB))
                        column.Item().AlignRight().Text($"PIB: {_firma.PIB}").FontSize(10).FontColor(Colors.Grey.Darken2);
                }
                column.Item().PaddingTop(5).AlignRight().Text($"Datum štampe: {DateTime.Now:dd.MM.yyyy.}").FontSize(8).FontColor(Colors.Grey.Darken1);
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
                    columns.ConstantColumn(20);  // Rbr
                    columns.ConstantColumn(55);  // Inv. Broj
                    columns.RelativeColumn();    // Naziv
                    columns.ConstantColumn(25);  // Kol.
                    columns.ConstantColumn(60);  // Faktura
                    columns.ConstantColumn(35);  // Grupa
                    columns.ConstantColumn(40);  // Stopa
                    columns.ConstantColumn(40);  // Konto
                    columns.ConstantColumn(25);  // OJ
                    columns.ConstantColumn(60);  // Nabavna vrednost
                    columns.ConstantColumn(60);  // Otpisana vrednost
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Rbr").Bold();
                    header.Cell().Element(CellStyle).Text("Inv. Broj").Bold();
                    header.Cell().Element(CellStyle).Text("Naziv osnovnog sredstva").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Kol.").Bold();
                    header.Cell().Element(CellStyle).Text("Faktura").Bold();
                    header.Cell().Element(CellStyle).Text("Am. Gr.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Stopa %").Bold();
                    header.Cell().Element(CellStyle).Text("Konto").Bold();
                    header.Cell().Element(CellStyle).Text("OJ").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Nabavna vr.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Otpis. vr.").Bold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.Background(Colors.Indigo.Darken4)
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(4)
                                        .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8f));
                    }
                });

                foreach (var stavka in _stavke)
                {
                    table.Cell().Element(CellStyle).Text(stavka.RedBroj.ToString());
                    table.Cell().Element(CellStyle).Text(stavka.InventarskiBroj);
                    table.Cell().Element(CellStyle).Text(stavka.Naziv);
                    table.Cell().Element(CellStyle).AlignRight().Text(stavka.Kolicina.ToString("N0"));
                    table.Cell().Element(CellStyle).Text(stavka.BrojFakture);
                    table.Cell().Element(CellStyle).Text(stavka.AmortizacionaGrupa);
                    table.Cell().Element(CellStyle).AlignRight().Text(stavka.StopaAmortizacije.ToString("N2"));
                    table.Cell().Element(CellStyle).Text(stavka.Konto);
                    table.Cell().Element(CellStyle).Text(stavka.ObracunskaJedinica.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(stavka.NabavnaVrednost.ToString("N2"));
                    table.Cell().Element(CellStyle).AlignRight().Text(stavka.OtpisanaVrednost.ToString("N2"));

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(0.5f)
                                        .BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(4)
                                        .DefaultTextStyle(x => x.FontSize(7.5f));
                    }
                }
            });

            var ukupnoNabavna = _stavke.Sum(x => x.NabavnaVrednost);
            var ukupnoOtpisana = _stavke.Sum(x => x.OtpisanaVrednost);
            
            col.Item().PaddingTop(10).AlignRight().Text(t =>
            {
                t.Span("Ukupna nabavna vr: ").FontSize(10);
                t.Span($"{ukupnoNabavna:N2}").Bold().FontSize(12);
                t.Span("   |   Ukupna otpisana vr: ").FontSize(10);
                t.Span($"{ukupnoOtpisana:N2}").Bold().FontSize(12);
            });
            
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
