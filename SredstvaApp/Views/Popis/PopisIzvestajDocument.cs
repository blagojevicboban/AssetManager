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
    private readonly Firma? _firma;
    private readonly List<ClanKomisije> _clanovi;

    public PopisIzvestajDocument(SredstvaData.Models.Popis popis, List<PopisnaStavka> stavke, Firma? firma, List<ClanKomisije> clanovi)
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
                column.Item().Text($"IZVEŠTAJ O POPISU OSNOVNIH SREDSTAVA").FontSize(14).SemiBold().FontColor(Colors.Indigo.Darken4);
                column.Item().Text($"Za godinu: {_popis.Godina}").FontSize(11).FontColor(Colors.Grey.Darken2);
                column.Item().Text($"Datum popisa: {_popis.DatumPopisa:dd.MM.yyyy}").FontSize(9).FontColor(Colors.Grey.Medium);
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
                column.Item().PaddingTop(5).AlignRight().Text($"Popis ID: {_popis.Id}").FontSize(9).SemiBold().FontColor(Colors.Grey.Lighten1);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(6).Column(column =>
        {
            var poRj = _stavke.GroupBy(s => s.Sredstvo.ObracunskaJedinica).OrderBy(g => g.Key).ToList();
            
            decimal ukupnoKnjVred = 0;
            decimal ukupnoProcVred = 0;

            foreach (var rjGroup in poRj)
            {
                decimal rjKnjVred = 0;
                decimal rjProcVred = 0;

                column.Item().PaddingTop(10).Text($"Obračunska jedinica: {rjGroup.Key}").FontSize(11).Bold().FontColor(Colors.Indigo.Darken3);

                var poKontu = rjGroup.GroupBy(s => s.Sredstvo.Konto).OrderBy(g => g.Key).ToList();

                foreach (var kontoGroup in poKontu)
                {
                    decimal kontoKnjVred = 0;
                    decimal kontoProcVred = 0;

                    column.Item().PaddingTop(5).Text($"Konto: {kontoGroup.Key}").FontSize(10).Bold().FontColor(Colors.Indigo.Darken2);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);  // Inv. Broj
                            columns.RelativeColumn();    // Naziv
                            columns.ConstantColumn(35);  // Knj. Kol
                            columns.ConstantColumn(35);  // Stv. Kol
                            columns.ConstantColumn(35);  // Razl. Kol
                            columns.ConstantColumn(65);  // Knj. Vred
                            columns.ConstantColumn(65);  // Proc. Vred
                            columns.ConstantColumn(65);  // Razl. Vred
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderStyle).Text("Inv. Broj").Bold();
                            header.Cell().Element(HeaderStyle).Text("Naziv sredstva").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Knj.kol").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Stv.kol").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Razlika").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Knj. Vred.").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Stv. Vred.").Bold();
                            header.Cell().Element(HeaderStyle).AlignRight().Text("Odstupanje").Bold();

                            static IContainer HeaderStyle(IContainer c)
                                => c.Background(Colors.Indigo.Darken4)
                                    .PaddingVertical(4).PaddingHorizontal(2)
                                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(7.5f));
                        });

                        foreach (var stavka in kontoGroup.OrderBy(x => x.Sredstvo.LegacySifra))
                        {
                            kontoKnjVred += stavka.KnjiznaVrednost;
                            kontoProcVred += stavka.ProcenjenaVrednost;

                            var kolRazlika = stavka.PopisanaKolicina - stavka.KnjiznaKolicina;
                            var vredRazlika = stavka.ProcenjenaVrednost - stavka.KnjiznaVrednost;

                            table.Cell().Element(RowStyle).Text(stavka.Sredstvo.InventarskiBroj);
                            table.Cell().Element(RowStyle).Text(stavka.Sredstvo.Naziv);
                            table.Cell().Element(RowStyle).AlignRight().Text(stavka.KnjiznaKolicina.ToString());
                            table.Cell().Element(RowStyle).AlignRight().Text(stavka.PopisanaKolicina.ToString());
                            table.Cell().Element(RowStyle).AlignRight().Text(kolRazlika != 0 ? kolRazlika.ToString() : "").FontColor(kolRazlika < 0 ? Colors.Orange.Darken2 : Colors.Green.Darken2);
                            table.Cell().Element(RowStyle).AlignRight().Text(stavka.KnjiznaVrednost.ToString("N2"));
                            table.Cell().Element(RowStyle).AlignRight().Text(stavka.ProcenjenaVrednost.ToString("N2"));
                            table.Cell().Element(RowStyle).AlignRight().Text(vredRazlika != 0 ? vredRazlika.ToString("N2") : "").FontColor(vredRazlika < 0 ? Colors.Orange.Darken2 : Colors.Green.Darken2);
                        }

                        static IContainer RowStyle(IContainer c)
                            => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(3).PaddingHorizontal(2)
                                .DefaultTextStyle(x => x.FontSize(7.5f));

                        rjKnjVred += kontoKnjVred;
                        rjProcVred += kontoProcVred;

                        // Zbirni red za Konto
                        table.Cell().ColumnSpan(5).Element(KontoSumStyle).Text($"Zbir za konto {kontoGroup.Key}").Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoKnjVred.ToString("N2")).Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoProcVred.ToString("N2")).Bold();
                        var odstupanje = kontoProcVred - kontoKnjVred;
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(odstupanje.ToString("N2")).Bold().FontColor(odstupanje < 0 ? Colors.Orange.Darken2 : (odstupanje > 0 ? Colors.Green.Darken2 : Colors.Black));

                        static IContainer KontoSumStyle(IContainer c)
                            => c.BorderTop(1).BorderBottom(1).BorderColor(Colors.Grey.Darken1)
                                .Background(Colors.Grey.Lighten3)
                                .PaddingVertical(4).PaddingHorizontal(4)
                                .DefaultTextStyle(x => x.SemiBold().FontSize(9f));
                    });
                }
                
                ukupnoKnjVred += rjKnjVred;
                ukupnoProcVred += rjProcVred;

                // Zbirni red za RJ
                column.Item().PaddingTop(4).Table(ojSumTable =>
                {
                    ojSumTable.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(50);
                        c.RelativeColumn();
                        c.ConstantColumn(35);
                        c.ConstantColumn(35);
                        c.ConstantColumn(35);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                    });
                    
                    ojSumTable.Cell().ColumnSpan(5).Element(OjSumStyle).Text($"Zbir za obračunsku jedinicu {rjGroup.Key}").Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(rjKnjVred.ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(rjProcVred.ToString("N2")).Bold();
                    var rjOdstupanje = rjProcVred - rjKnjVred;
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(rjOdstupanje.ToString("N2")).Bold().FontColor(rjOdstupanje < 0 ? Colors.Orange.Darken2 : (rjOdstupanje > 0 ? Colors.Green.Darken2 : Colors.Black));
                    
                    static IContainer OjSumStyle(IContainer c)
                        => c.Background(Colors.Indigo.Lighten4)
                            .BorderTop(1).BorderColor(Colors.Indigo.Darken3)
                            .PaddingVertical(5).PaddingHorizontal(4)
                            .DefaultTextStyle(x => x.FontSize(8.5f));
                });
            }

            // Apsolutni zbir
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
                table.Cell().Element(GrandSumStyle).AlignRight().Text(ukupnoKnjVred.ToString("N2")).Bold();
                table.Cell().Element(GrandSumStyle).AlignRight().Text(ukupnoProcVred.ToString("N2")).Bold();
                var totalOdstupanje = ukupnoProcVred - ukupnoKnjVred;
                table.Cell().Element(GrandSumStyle).AlignRight().Text(totalOdstupanje.ToString("N2")).Bold().FontColor(totalOdstupanje < 0 ? Colors.Orange.Darken2 : (totalOdstupanje > 0 ? Colors.Green.Darken2 : Colors.Black));
                
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
