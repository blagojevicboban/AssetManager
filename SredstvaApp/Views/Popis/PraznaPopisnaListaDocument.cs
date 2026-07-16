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

            decimal grandKnj = 0;
            decimal grandProc = 0;
            decimal grandNabavna = 0;
            decimal grandOtpisana = 0;
            decimal grandSadasnja = 0;

            foreach (var ojGroup in poObracunskimJedinicama)
            {
                decimal ojNabavna = 0;
                decimal ojOtpisana = 0;
                decimal ojSadasnja = 0;

                column.Item().PaddingTop(10).PaddingBottom(5).Text($"Popisno mesto / Obračunska jedinica: {ojGroup.Key}")
                    .FontSize(12).Bold().FontColor(_primaryColor).Underline();

                var poKontima = ojGroup.GroupBy(s => s.Sredstvo.Konto).OrderBy(g => g.Key).ToList();

                foreach (var kontoGroup in poKontima)
                {
                    decimal kontoNabavna = 0;
                    decimal kontoOtpisana = 0;
                    decimal kontoSadasnja = 0;

                    column.Item().PaddingBottom(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // RBR
                            columns.ConstantColumn(50);  // Šifra
                            columns.ConstantColumn(60);  // Inv. Broj
                            columns.RelativeColumn();    // Naziv
                            columns.ConstantColumn(40);  // Konto
                            columns.ConstantColumn(55);  // Kolicina
                            columns.ConstantColumn(75);  // Nabavna
                            columns.ConstantColumn(75);  // Otpisana
                            columns.ConstantColumn(75);  // Sadasnja
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("R.Br");
                            header.Cell().Element(HeaderStyle).Text("Šifra");
                            header.Cell().Element(HeaderStyle).Text("Inv. Broj");
                            header.Cell().Element(HeaderStyle).Text("Naziv osnovnog sredstva");
                            header.Cell().Element(HeaderStyle).Text("Konto");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Količina");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Nabavna vred.");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Otpisana vred.");
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Sadašnja vred.");

                            static IContainer HeaderStyle(IContainer container)
                            {
                                return container.DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8.5f)).PaddingVertical(4).PaddingHorizontal(2).Background("#2B4B80").BorderBottom(1).BorderColor(Colors.Black);
                            }
                        });

                        int rbr = 1;
                        foreach (var stavka in kontoGroup.OrderBy(x => x.Sredstvo.InventarskiBrojSort))
                        {
                            var nabavna = stavka.Sredstvo.NabavnaVrednost;
                            var otpisana = stavka.Sredstvo.IspravkaVrednosti;
                            var sadasnja = stavka.Sredstvo.SadasnjaVrednost;

                            kontoNabavna += nabavna;
                            kontoOtpisana += otpisana;
                            kontoSadasnja += sadasnja;

                            table.Cell().Element(CellStyle).Text(rbr.ToString());
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.LegacySifra.ToString());
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.InventarskiBroj);
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.Naziv);
                            table.Cell().Element(CellStyle).Text(stavka.Sredstvo.Konto);
                            table.Cell().Element(CellStyle).AlignRight().Text(stavka.Sredstvo.Kolicina.ToString());
                            
                            table.Cell().Element(CellStyle).AlignRight().Text(nabavna.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignRight().Text(otpisana.ToString("N2"));
                            table.Cell().Element(CellStyle).AlignRight().Text(sadasnja.ToString("N2"));

                            rbr++;
                        }

                        ojNabavna += kontoNabavna;
                        ojOtpisana += kontoOtpisana;
                        ojSadasnja += kontoSadasnja;

                        // Ukupno za konto
                        table.Cell().ColumnSpan(6).Element(SumStyle).AlignRight().Text($"Ukupno za konto : {kontoGroup.Key}").Bold();
                        table.Cell().Element(SumStyle).AlignRight().Text(kontoNabavna.ToString("N2")).Bold();
                        table.Cell().Element(SumStyle).AlignRight().Text(kontoOtpisana.ToString("N2")).Bold();
                        table.Cell().Element(SumStyle).AlignRight().Text(kontoSadasnja.ToString("N2")).Bold();

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2).DefaultTextStyle(x => x.FontSize(8.5f));
                        }
                        
                        static IContainer SumStyle(IContainer container)
                        {
                            return container.Background(Colors.Indigo.Lighten5).BorderTop(1).BorderColor(Colors.Indigo.Darken2).PaddingVertical(4).PaddingHorizontal(2).DefaultTextStyle(x => x.FontSize(9f));
                        }
                    });
                }
                
                grandNabavna += ojNabavna;
                grandOtpisana += ojOtpisana;
                grandSadasnja += ojSadasnja;

                column.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(75);
                        columns.ConstantColumn(75);
                    });
                    
                    table.Cell().Element(OjSumStyle).AlignRight().PaddingRight(5).Text($"Ukupno za obračunsku jedinicu : {ojGroup.Key}").Bold();
                    table.Cell().Element(OjSumStyle).AlignRight().Text(ojNabavna.ToString("N2")).Bold();
                    table.Cell().Element(OjSumStyle).AlignRight().Text(ojOtpisana.ToString("N2")).Bold();
                    table.Cell().Element(OjSumStyle).AlignRight().Text(ojSadasnja.ToString("N2")).Bold();
                    
                    static IContainer OjSumStyle(IContainer container)
                    {
                        return container.Background(Colors.Indigo.Lighten4).BorderTop(1).BorderColor(Colors.Indigo.Darken3).PaddingVertical(5).PaddingHorizontal(2).DefaultTextStyle(x => x.FontSize(9.5f));
                    }
                });
                
                column.Item().PaddingTop(20).PaddingBottom(40).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text("Služba osnovnih sredstava\n___________________________").FontSize(10);
                    row.RelativeItem().AlignCenter().Text("Računopolagač\n___________________________").FontSize(10);
                    row.RelativeItem().AlignCenter().Text("Članovi komisije\n1. _______________________\n2. _______________________").FontSize(10);
                });
            }

            column.Item().PaddingTop(15).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(75);
                    columns.ConstantColumn(75);
                    columns.ConstantColumn(75);
                });
                
                table.Cell().Element(GrandSumStyle).AlignRight().PaddingRight(5).Text("UKUPNO :").Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandNabavna.ToString("N2")).Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandOtpisana.ToString("N2")).Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandSadasnja.ToString("N2")).Bold();
                
                static IContainer GrandSumStyle(IContainer container)
                {
                    return container.Background(Colors.Grey.Lighten3).BorderTop(2).BorderBottom(2).BorderColor(Colors.Black).PaddingVertical(6).PaddingHorizontal(2).DefaultTextStyle(x => x.FontSize(10f));
                }
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
