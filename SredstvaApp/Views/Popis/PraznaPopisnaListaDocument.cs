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
    private readonly Firma? _firma;

    public PraznaPopisnaListaDocument(SredstvaData.Models.Popis popis, List<PopisnaStavka> stavke, Firma? firma)
    {
        _popis = popis;
        _stavke = stavke;
        _firma = firma;
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
                column.Item().Text($"POPISNA LISTA OSNOVNIH SREDSTAVA").FontSize(13).SemiBold().FontColor(Colors.Indigo.Darken4);
                column.Item().Text($"Za godinu: {_popis.Godina}").FontSize(12).FontColor(Colors.Grey.Darken2);
                column.Item().Text($"Datum popisa: {_popis.DatumPopisa:dd.MM.yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);
            });

            row.ConstantItem(250).AlignRight().Column(column =>
            {
                if (_firma != null)
                {
                    column.Item().AlignRight().Text(_firma.Naziv).FontSize(11).SemiBold().FontColor(Colors.Black);
                    if (!string.IsNullOrEmpty(_firma.Mesto))
                        column.Item().AlignRight().Text(_firma.Mesto).FontSize(10).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrEmpty(_firma.PIB))
                        column.Item().AlignRight().Text($"PIB: {_firma.PIB}").FontSize(10).FontColor(Colors.Grey.Darken2);
                }
                column.Item().PaddingTop(5).AlignRight().Text($"Popis ID: {_popis.Id}").FontSize(10).SemiBold().FontColor(Colors.Grey.Lighten1);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(6).Column(column =>
        {
            var poObracunskimJedinicama = _stavke.GroupBy(s => s.Sredstvo.ObracunskaJedinica).OrderBy(g => g.Key).ToList();

            decimal grandNabavna = 0;
            decimal grandOtpisana = 0;
            decimal grandSadasnja = 0;

            foreach (var ojGroup in poObracunskimJedinicama)
            {
                decimal ojNabavna = 0;
                decimal ojOtpisana = 0;
                decimal ojSadasnja = 0;

                column.Item().PaddingTop(10).Text($"Obračunska jedinica: {ojGroup.Key}").FontSize(12).Bold().FontColor(Colors.Indigo.Darken3);

                var poKontima = ojGroup.GroupBy(s => s.Sredstvo.Konto).OrderBy(g => g.Key).ToList();

                foreach (var kontoGroup in poKontima)
                {
                    decimal kontoNabavna = 0;
                    decimal kontoOtpisana = 0;
                    decimal kontoSadasnja = 0;

                    column.Item().PaddingTop(5).Text($"Konto: {kontoGroup.Key}").FontSize(11).Bold().FontColor(Colors.Indigo.Darken2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);  // RBR
                            columns.ConstantColumn(50);  // Šifra
                            columns.ConstantColumn(60);  // Inv. Broj
                            columns.RelativeColumn();    // Naziv
                            columns.ConstantColumn(55);  // Kolicina
                            columns.ConstantColumn(80);  // Nabavna
                            columns.ConstantColumn(80);  // Otpisana
                            columns.ConstantColumn(80);  // Sadasnja
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("R.Br").Bold();
                            header.Cell().Element(HeaderStyle).Text("Šifra").Bold();
                            header.Cell().Element(HeaderStyle).Text("Inv. Broj").Bold();
                            header.Cell().Element(HeaderStyle).Text("Naziv osnovnog sredstva").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Količina").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Nabavna vred.").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Otpisana vred.").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Sadašnja vred.").Bold();

                            static IContainer HeaderStyle(IContainer c)
                                => c.Background(Colors.Indigo.Darken4)
                                    .PaddingVertical(4).PaddingHorizontal(4)
                                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8.5f));
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

                            table.Cell().Element(RowStyle).Text(rbr.ToString());
                            table.Cell().Element(RowStyle).Text(stavka.Sredstvo.LegacySifra.ToString());
                            table.Cell().Element(RowStyle).Text(stavka.Sredstvo.InventarskiBroj);
                            table.Cell().Element(RowStyle).Text(stavka.Sredstvo.Naziv);
                            table.Cell().Element(RowStyle).AlignRight().Text(stavka.Sredstvo.Kolicina.ToString());
                            
                            table.Cell().Element(RowStyle).AlignRight().Text(nabavna.ToString("N2"));
                            table.Cell().Element(RowStyle).AlignRight().Text(otpisana.ToString("N2"));
                            table.Cell().Element(RowStyle).AlignRight().Text(sadasnja.ToString("N2"));

                            static IContainer RowStyle(IContainer c)
                                => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .PaddingVertical(3).PaddingHorizontal(4)
                                    .DefaultTextStyle(x => x.FontSize(8.5f));
                            rbr++;
                        }

                        ojNabavna += kontoNabavna;
                        ojOtpisana += kontoOtpisana;
                        ojSadasnja += kontoSadasnja;

                        // Ukupno za konto
                        table.Cell().ColumnSpan(5).Element(KontoSumStyle).Text($"Zbir za konto {kontoGroup.Key}").Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoNabavna.ToString("N2")).Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoOtpisana.ToString("N2")).Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoSadasnja.ToString("N2")).Bold();

                        static IContainer KontoSumStyle(IContainer c)
                            => c.BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Darken1)
                                .Background(Colors.Grey.Lighten3)
                                .PaddingVertical(4).PaddingHorizontal(4)
                                .DefaultTextStyle(x => x.SemiBold().FontSize(9f));
                    });
                }
                
                grandNabavna += ojNabavna;
                grandOtpisana += ojOtpisana;
                grandSadasnja += ojSadasnja;

                column.Item().PaddingTop(4).Table(ojSumTable =>
                {
                    ojSumTable.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(30);
                        c.ConstantColumn(50);
                        c.ConstantColumn(60);
                        c.RelativeColumn();
                        c.ConstantColumn(55);
                        c.ConstantColumn(80);
                        c.ConstantColumn(80);
                        c.ConstantColumn(80);
                    });
                    
                    ojSumTable.Cell().ColumnSpan(5).Element(OjSumStyle).Text($"Zbir za obračunsku jedinicu {ojGroup.Key}").Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojNabavna.ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojOtpisana.ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojSadasnja.ToString("N2")).Bold();
                    
                    static IContainer OjSumStyle(IContainer c)
                        => c.Background(Colors.Indigo.Lighten4)
                            .BorderTop(1).BorderColor(Colors.Indigo.Darken3)
                            .PaddingVertical(5).PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.FontSize(9.5f));
                });
            }

            column.Item().PaddingTop(15).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(80);
                    c.ConstantColumn(80);
                    c.ConstantColumn(80);
                });
                
                table.Cell().Element(GrandSumStyle).AlignRight().PaddingRight(5).Text("UKUPAN POPIS:").Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandNabavna.ToString("N2")).Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandOtpisana.ToString("N2")).Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandSadasnja.ToString("N2")).Bold();
                
                static IContainer GrandSumStyle(IContainer c)
                    => c.Background(Colors.Grey.Lighten3)
                        .BorderTop(2).BorderBottom(2).BorderColor(Colors.Black)
                        .PaddingVertical(6).PaddingHorizontal(4)
                        .DefaultTextStyle(x => x.FontSize(10f));
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
