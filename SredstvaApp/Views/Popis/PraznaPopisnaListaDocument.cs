using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SredstvaData.Models;

namespace SredstvaApp.Views.Popis;

public class PraznaPopisnaListaDocument : IDocument
{
    private readonly SredstvaData.Models.Popis _popis;
    private readonly List<PopisnaStavka> _stavke;
    private readonly string _primaryColor = "#2B4B80";

    public PraznaPopisnaListaDocument(SredstvaData.Models.Popis popis, List<PopisnaStavka> stavke)
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
                column.Item().Text($"POPISNA LISTA OSNOVNIH SREDSTAVA").FontSize(16).SemiBold().FontColor(_primaryColor);
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
            var poObracunskimJedinicama = _stavke.GroupBy(s => s.Sredstvo.ObracunskaJedinica).OrderBy(g => g.Key).ToList();

            foreach (var ojGroup in poObracunskimJedinicama)
            {
                column.Item().PaddingTop(10).PaddingBottom(5).Text($"Popisno mesto / Obračunska jedinica: {ojGroup.Key}")
                    .FontSize(12).Bold().FontColor(_primaryColor).Underline();

                var poKontima = ojGroup.GroupBy(s => s.Sredstvo.Konto).OrderBy(g => g.Key).ToList();

                foreach (var kontoGroup in poKontima)
                {
                    column.Item().PaddingTop(5).PaddingBottom(5).Text($"Konto: {kontoGroup.Key}")
                        .FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);

                    column.Item().PaddingBottom(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // RBR
                            columns.ConstantColumn(50);  // Šifra
                            columns.ConstantColumn(60);  // Inv. Broj
                            columns.RelativeColumn();    // Naziv
                            columns.ConstantColumn(40);  // Knj. Količina
                            columns.ConstantColumn(45);  // Stvarna
                            columns.ConstantColumn(45);  // Višak
                            columns.ConstantColumn(45);  // Manjak
                            columns.ConstantColumn(70);  // Cena (Knj. Vrednost)
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("R.Br");
                            header.Cell().Element(HeaderStyle).Text("Šifra");
                            header.Cell().Element(HeaderStyle).Text("Inv. Broj");
                            header.Cell().Element(HeaderStyle).Text("Naziv osnovnog sredstva");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Knj. Kol.");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Stv. Kol.");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Višak");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Manjak");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Knj. Vred.");

                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White)).PaddingVertical(4).PaddingHorizontal(2).Background("#2B4B80").BorderBottom(1).BorderColor(Colors.Black);
                            }
                        });

                        int rbr = 1;
                        foreach (var stavka in kontoGroup)
                        {
                            table.Cell().Element(CellStyle).Text(rbr.ToString());
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.LegacySifra.ToString());
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.InventarskiBroj);
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.Naziv);
                            table.Cell().Element(CellStyle).AlignRight().Text(stavka.KnjiznaKolicina.ToString());
                            
                            table.Cell().Element(EmptyCellStyle).Text("");
                            table.Cell().Element(EmptyCellStyle).Text("");
                            table.Cell().Element(EmptyCellStyle).Text("");
                            
                            table.Cell().Element(CellStyle).AlignRight().Text(stavka.KnjiznaVrednost.ToString("N2"));

                            rbr++;
                        }

                        // Ukupno za konto
                        table.Cell().ColumnSpan(8).Element(SumStyle).AlignRight().Text($"Ukupno knj. vrednost za konto {kontoGroup.Key}:").Bold();
                        table.Cell().Element(SumStyle).AlignRight().Text(kontoGroup.Sum(s => s.KnjiznaVrednost).ToString("N2")).Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);
                        }
                        
                        static IContainer EmptyCellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(2);
                        }

                        static IContainer SumStyle(IContainer container)
                        {
                            return container.Background(Colors.Indigo.Lighten5).BorderTop(1).BorderColor(Colors.Indigo.Darken2).PaddingVertical(4).PaddingHorizontal(2);
                        }
                    });
                }
                
                column.Item().PaddingTop(20).PaddingBottom(40).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text("Služba osnovnih sredstava\n___________________________").FontSize(10);
                    row.RelativeItem().AlignCenter().Text("Računopolagač\n___________________________").FontSize(10);
                    row.RelativeItem().AlignCenter().Text("Članovi komisije\n1. _______________________\n2. _______________________").FontSize(10);
                });
            }
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
