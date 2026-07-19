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
    private readonly List<ClanKomisije> _clanovi;

    public PraznaPopisnaListaDocument(SredstvaData.Models.Popis popis, List<PopisnaStavka> stavke, Firma? firma, List<ClanKomisije> clanovi)
    {
        _popis = popis;
        _stavke = stavke;
        _firma = firma;
        _clanovi = clanovi;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4.Portrait());
                page.Margin(0.5f, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));

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
                column.Item().Text($"POPISNA LISTA OSNOVNIH SREDSTAVA").FontSize(12).SemiBold().FontColor(Colors.Indigo.Darken4);
                column.Item().Text($"Za godinu: {_popis.Godina}").FontSize(10).FontColor(Colors.Grey.Darken2);
                column.Item().Text($"Datum popisa: {_popis.DatumPopisa:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
                column.Item().Text($"Popis ID: {_popis.Id}").FontSize(9).SemiBold().FontColor(Colors.Grey.Lighten1);
            });

            row.ConstantItem(200).AlignRight().Column(column =>
            {
                if (_firma != null)
                {
                    column.Item().AlignRight().Text(_firma.Naziv).FontSize(11).SemiBold().FontColor(Colors.Black);
                    if (!string.IsNullOrEmpty(_firma.Mesto))
                        column.Item().AlignRight().Text(_firma.Mesto).FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrEmpty(_firma.PIB))
                        column.Item().AlignRight().Text($"PIB: {_firma.PIB}").FontSize(9).FontColor(Colors.Grey.Darken2);
                }
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

                column.Item().PaddingTop(10).Text($"Obračunska jedinica: {ojGroup.Key}").FontSize(11).Bold().FontColor(Colors.Indigo.Darken3);

                var poKontima = ojGroup.GroupBy(s => s.Sredstvo.Konto).OrderBy(g => g.Key).ToList();

                foreach (var kontoGroup in poKontima)
                {
                    decimal kontoNabavna = 0;
                    decimal kontoOtpisana = 0;
                    decimal kontoSadasnja = 0;

                    column.Item().PaddingTop(5).Text($"Konto: {kontoGroup.Key}").FontSize(10).Bold().FontColor(Colors.Indigo.Darken2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);  // RBR
                            columns.ConstantColumn(40);  // Šifra
                            columns.ConstantColumn(50);  // Inv. Broj
                            columns.RelativeColumn();    // Naziv
                            columns.ConstantColumn(40);  // Kolicina
                            columns.ConstantColumn(65);  // Nabavna
                            columns.ConstantColumn(65);  // Otpisana
                            columns.ConstantColumn(65);  // Sadasnja
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
                                    .PaddingVertical(4).PaddingHorizontal(2)
                                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(7.5f));
                        });

                        int rbr = 1;
                        foreach (var stavka in kontoGroup.OrderBy(x => x.Sredstvo.LegacySifra))
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
                                    .PaddingVertical(3).PaddingHorizontal(2)
                                    .DefaultTextStyle(x => x.FontSize(7.5f));
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
                                .DefaultTextStyle(x => x.SemiBold().FontSize(8f));
                    });
                }
                
                grandNabavna += ojNabavna;
                grandOtpisana += ojOtpisana;
                grandSadasnja += ojSadasnja;

                column.Item().PaddingTop(4).Table(ojSumTable =>
                {
                    ojSumTable.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(25);
                        c.ConstantColumn(40);
                        c.ConstantColumn(50);
                        c.RelativeColumn();
                        c.ConstantColumn(40);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                    });
                    
                    ojSumTable.Cell().ColumnSpan(5).Element(OjSumStyle).Text($"Zbir za obračunsku jedinicu {ojGroup.Key}").Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojNabavna.ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojOtpisana.ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojSadasnja.ToString("N2")).Bold();
                    
                    static IContainer OjSumStyle(IContainer c)
                        => c.Background(Colors.Indigo.Lighten4)
                            .BorderTop(1).BorderColor(Colors.Indigo.Darken3)
                            .PaddingVertical(5).PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.FontSize(8.5f));
                });
            }

            column.Item().PaddingTop(15).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn();
                    c.ConstantColumn(65);
                    c.ConstantColumn(65);
                    c.ConstantColumn(65);
                });
                
                table.Cell().Element(GrandSumStyle).AlignRight().PaddingRight(5).Text("UKUPAN POPIS:").Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandNabavna.ToString("N2")).Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandOtpisana.ToString("N2")).Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(grandSadasnja.ToString("N2")).Bold();
                
                static IContainer GrandSumStyle(IContainer c)
                    => c.Background(Colors.Grey.Lighten3)
                        .BorderTop(2).BorderBottom(2).BorderColor(Colors.Black)
                        .PaddingVertical(6).PaddingHorizontal(4)
                        .DefaultTextStyle(x => x.FontSize(8.5f));
            });
            
            column.Item().PaddingTop(30).PaddingBottom(20).Row(row =>
            {
                row.RelativeItem().AlignCenter().Text("Služba osnovnih sredstava\n___________________________").FontSize(10);
                row.RelativeItem().AlignCenter().Text("Računopolagač\n___________________________").FontSize(10);
                
                row.RelativeItem().AlignCenter().Column(c => 
                {
                    c.Item().AlignCenter().Text("Članovi komisije").FontSize(10).SemiBold();
                    if (_clanovi == null || _clanovi.Count == 0)
                    {
                        c.Item().AlignCenter().PaddingTop(10).Text("1. _______________________").FontSize(10);
                        c.Item().AlignCenter().PaddingTop(10).Text("2. _______________________").FontSize(10);
                    }
                    else
                    {
                        for (int i = 0; i < _clanovi.Count; i++)
                        {
                            var clan = _clanovi[i];
                            c.Item().AlignCenter().PaddingTop(15).Text($"{i + 1}. _______________________").FontSize(10);
                            c.Item().AlignCenter().Text($"{clan.ImePrezime} ({clan.Uloga})").FontSize(9);
                        }
                    }
                });
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
