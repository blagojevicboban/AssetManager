using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SredstvaData.Models;

namespace SredstvaApp.Views.Popis;

public class PopisIzvestajDocument : IDocument
{
    private readonly SredstvaData.Models.Popis _popis;
    private readonly List<PopisnaStavka> _stavke;
    private readonly string _primaryColor = "#2B4B80"; 
    private readonly string _accentColor = "#E63946"; 

    public PopisIzvestajDocument(SredstvaData.Models.Popis popis, List<PopisnaStavka> stavke)
    {
        _popis = popis;
        _stavke = stavke;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

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
                column.Item().Text($"ZAVRŠNI IZVEŠTAJ O POPISU (SA RAZLIKAMA)").FontSize(16).SemiBold().FontColor(_primaryColor);
                column.Item().Text($"Za godinu: {_popis.Godina}").FontSize(12).FontColor(Colors.Grey.Darken2);
                column.Item().Text($"Datum popisa: {_popis.DatumPopisa:dd.MM.yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);
            });
            row.ConstantItem(120).AlignRight().Text($"Popis ID: {_popis.Id}").FontSize(14).SemiBold().FontColor(Colors.Grey.Lighten1);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            var poRj = _stavke.GroupBy(s => s.Sredstvo.ObracunskaJedinica).OrderBy(g => g.Key).ToList();
            
            decimal ukupnoKnjVred = 0;
            decimal ukupnoProcVred = 0;

            foreach (var rjGroup in poRj)
            {
                decimal rjKnjVred = 0;
                decimal rjProcVred = 0;

                column.Item().PaddingTop(10).PaddingBottom(5).Text($"Obračunska jedinica: {rjGroup.Key}")
                    .FontSize(12).Bold().FontColor(_primaryColor).Underline();

                var poKontu = rjGroup.GroupBy(s => s.Sredstvo.Konto).OrderBy(g => g.Key).ToList();

                column.Item().PaddingBottom(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(60);  // Inv. Broj
                        columns.RelativeColumn();    // Naziv
                        columns.ConstantColumn(50);  // Konto
                        columns.ConstantColumn(50);  // Knj. Kol
                        columns.ConstantColumn(50);  // Stv. Kol
                        columns.ConstantColumn(50);  // Razl. Kol
                        columns.ConstantColumn(75);  // Knj. Vred
                        columns.ConstantColumn(75);  // Proc. Vred
                        columns.ConstantColumn(75);  // Razl. Vred
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Inv. Broj");
                        header.Cell().Element(HeaderStyle).Text("Naziv osnovnog sredstva");
                        header.Cell().Element(HeaderStyle).Text("Konto");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Knj. Kol.");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Stv. Kol.");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Razlika");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Knj. Vred.");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Stv. Vred.");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Odstupanje");

                        static IContainer HeaderStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White)).PaddingVertical(4).PaddingHorizontal(2).Background("#2B4B80").BorderBottom(1).BorderColor(Colors.Black);
                        }
                    });

                    foreach (var kontoGroup in poKontu)
                    {
                        decimal kontoKnjVred = 0;
                        decimal kontoProcVred = 0;

                        foreach (var stavka in kontoGroup.OrderBy(x => x.Sredstvo.InventarskiBrojSort))
                        {
                            kontoKnjVred += stavka.KnjiznaVrednost;
                            kontoProcVred += stavka.ProcenjenaVrednost;

                            var kolRazlika = stavka.PopisanaKolicina - stavka.KnjiznaKolicina;
                            var vredRazlika = stavka.ProcenjenaVrednost - stavka.KnjiznaVrednost;

                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.InventarskiBroj);
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.Naziv);
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.Konto);
                            table.Cell().Element(CellStyle).AlignRight().Text(stavka.KnjiznaKolicina.ToString());
                            table.Cell().Element(CellStyle).AlignRight().Text(stavka.PopisanaKolicina.ToString());
                            table.Cell().Element(CellStyle).AlignRight().Text(kolRazlika != 0 ? kolRazlika.ToString() : "").FontColor(kolRazlika < 0 ? _accentColor : Colors.Green.Darken2);
                            table.Cell().Element(CellStyle).AlignRight().Text(stavka.KnjiznaVrednost.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignRight().Text(stavka.ProcenjenaVrednost.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignRight().Text(vredRazlika != 0 ? vredRazlika.ToString("N2") : "").FontColor(vredRazlika < 0 ? _accentColor : Colors.Green.Darken2);
                        }

                        rjKnjVred += kontoKnjVred;
                        rjProcVred += kontoProcVred;

                        // Zbirni red za Konto
                        table.Cell().ColumnSpan(6).Element(SubTotalStyle).AlignRight().Text($"UKUPNO ZA KONTO {kontoGroup.Key}:");
                        table.Cell().Element(SubTotalStyle).AlignRight().Text(kontoKnjVred.ToString("N2"));
                        table.Cell().Element(SubTotalStyle).AlignRight().Text(kontoProcVred.ToString("N2"));
                        var odstupanje = kontoProcVred - kontoKnjVred;
                        table.Cell().Element(SubTotalStyle).AlignRight().Text(odstupanje.ToString("N2")).FontColor(odstupanje < 0 ? _accentColor : (odstupanje > 0 ? Colors.Green.Darken2 : Colors.Black));
                    }
                    
                    ukupnoKnjVred += rjKnjVred;
                    ukupnoProcVred += rjProcVred;

                    // Zbirni red za RJ
                    table.Cell().ColumnSpan(6).Element(TotalStyle).AlignRight().Text($"UKUPNO ZA RJ {rjGroup.Key}:");
                    table.Cell().Element(TotalStyle).AlignRight().Text(rjKnjVred.ToString("N2"));
                    table.Cell().Element(TotalStyle).AlignRight().Text(rjProcVred.ToString("N2"));
                    var rjOdstupanje = rjProcVred - rjKnjVred;
                    table.Cell().Element(TotalStyle).AlignRight().Text(rjOdstupanje.ToString("N2")).FontColor(rjOdstupanje < 0 ? _accentColor : (rjOdstupanje > 0 ? Colors.Green.Darken2 : Colors.White));

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);
                    }
                    static IContainer SubTotalStyle(IContainer container)
                    {
                        return container.BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten3).PaddingVertical(4).PaddingHorizontal(2).DefaultTextStyle(x => x.SemiBold());
                    }
                    static IContainer TotalStyle(IContainer container)
                    {
                        return container.BorderTop(2).BorderColor(Colors.Black).Background("#2B4B80").PaddingVertical(6).PaddingHorizontal(2).DefaultTextStyle(x => x.Bold().FontColor(Colors.White));
                    }
                });
            }

            // Apsolutni zbir
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(100);
                    columns.ConstantColumn(100);
                });
                
                table.Cell().AlignRight().PaddingRight(10).Text("UKUPAN POPIS SVIH SREDSTAVA:").FontSize(12).Bold();
                table.Cell().AlignRight().Text(ukupnoKnjVred.ToString("N2")).FontSize(12).Bold();
                table.Cell().AlignRight().Text(ukupnoProcVred.ToString("N2")).FontSize(12).Bold();
                
                var totalOdstupanje = ukupnoProcVred - ukupnoKnjVred;
                table.Cell().AlignRight().Text(totalOdstupanje.ToString("N2")).FontSize(12).Bold().FontColor(totalOdstupanje < 0 ? _accentColor : (totalOdstupanje > 0 ? Colors.Green.Darken2 : Colors.Black));
            });
            
            column.Item().PaddingTop(30).PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().AlignCenter().Text("Služba osnovnih sredstava\n___________________________").FontSize(10);
                row.RelativeItem().AlignCenter().Text("Računopolagač\n___________________________").FontSize(10);
                row.RelativeItem().AlignCenter().Text("Članovi komisije\n1. _______________________\n2. _______________________").FontSize(10);
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
