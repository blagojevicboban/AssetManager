using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SredstvaApp.Views.Amortizacija;

public class AmortizacijaDocument : IDocument
{
    private readonly List<AmortizacijaResultViewModel> _rezultati;
    private readonly SredstvaData.Models.Firma? _firma;
    private readonly DateTime _odDatuma;
    private readonly DateTime _doDatuma;
    private readonly string _primaryColor = "#2B4B80";

    public AmortizacijaDocument(List<AmortizacijaResultViewModel> rezultati, SredstvaData.Models.Firma? firma, DateTime odDatuma, DateTime doDatuma)
    {
        _rezultati = rezultati;
        _firma = firma;
        _odDatuma = odDatuma;
        _doDatuma = doDatuma;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Portrait());
            page.Margin(0.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(8).FontFamily("Calibri"));

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
                column.Item().Text($"OBRAČUN AMORTIZACIJE OSNOVNIH SREDSTAVA").FontSize(14).SemiBold().FontColor(_primaryColor);
                column.Item().Text($"Za period: {_odDatuma:dd.MM.yyyy} - {_doDatuma:dd.MM.yyyy}").FontSize(11).FontColor(Colors.Grey.Darken2);
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
        container.PaddingVertical(6).Column(col =>
        {
            var ojGroups = _rezultati.GroupBy(x => x.ObracunskaJedinica).OrderBy(g => g.Key).ToList();

            foreach (var ojGroup in ojGroups)
            {
                col.Item().PaddingTop(10).Text($"Obračunska jedinica: {ojGroup.Key}").FontSize(11).Bold().FontColor(Colors.Indigo.Darken3);

                var kontoGroups = ojGroup.GroupBy(x => x.Konto).OrderBy(g => g.Key).ToList();

                foreach (var kontoGroup in kontoGroups)
                {
                    col.Item().PaddingTop(5).Text($"Konto: {kontoGroup.Key}").FontSize(10).Bold().FontColor(Colors.Indigo.Darken2);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50);  // Inv. Br.
                            columns.RelativeColumn();    // Naziv
                            columns.ConstantColumn(35);  // Stopa %
                            columns.ConstantColumn(65);  // Nabavna Vr.
                            columns.ConstantColumn(65);  // Prethodna isp.
                            columns.ConstantColumn(65);  // Nova Amortizacija
                            columns.ConstantColumn(65);  // Nova Ispravka ukupno
                            columns.ConstantColumn(65);  // Sadašnja vrednost
                        });

                        // Header tabele
                        table.Header(header =>
                        {
                            header.Cell().Element(HdrStyle).Text("Šifra").Bold();
                            header.Cell().Element(HdrStyle).Text("Naziv sredstva").Bold();
                            header.Cell().Element(HdrStyle).AlignRight().Text("Stopa %").Bold();
                            header.Cell().Element(HdrStyle).AlignRight().Text("Nabavna Vr.").Bold();
                            header.Cell().Element(HdrStyle).AlignRight().Text("Preth. isp.").Bold();
                            header.Cell().Element(HdrStyle).AlignRight().Text("Nova amort.").Bold();
                            header.Cell().Element(HdrStyle).AlignRight().Text("Isp. ukupno").Bold();
                            header.Cell().Element(HdrStyle).AlignRight().Text("Sadašnja Vr.").Bold();

                            static IContainer HdrStyle(IContainer c)
                                => c.Background(Colors.Indigo.Darken4)
                                    .PaddingVertical(4).PaddingHorizontal(2)
                                    .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(7.5f));
                        });

                        var amGroups = kontoGroup.GroupBy(x => x.AmortizacionaGrupa).OrderBy(g => g.Key).ToList();

                        foreach (var amGroup in amGroups)
                        {
                            foreach (var r in amGroup.OrderBy(x => x.LegacySifra))
                            {
                                bool imaAmort = r.NovaAmortizacija > 0;

                                table.Cell().Element(RowStyle).Text(r.LegacySifra.ToString());
                                table.Cell().Element(RowStyle).Text(r.Naziv);
                                table.Cell().Element(RowStyle).AlignRight().Text(r.StopaAmortizacije.ToString("N2"));
                                table.Cell().Element(RowStyle).AlignRight().Text(r.NabavnaVrednost.ToString("N2"));
                                table.Cell().Element(RowStyle).AlignRight().Text(r.PrethodnaIspravka.ToString("N2"));

                                table.Cell().Element(RowStyle).AlignRight().Text(t =>
                                {
                                    var span = t.Span(r.NovaAmortizacija.ToString("N2"))
                                         .FontColor(imaAmort ? Colors.Orange.Darken2 : Colors.Grey.Darken1);
                                    if (imaAmort) span.Bold();
                                });

                                table.Cell().Element(RowStyle).AlignRight().Text(r.NovaIspravkaUkupno.ToString("N2"));

                                table.Cell().Element(RowStyle).AlignRight().Text(t =>
                                {
                                    var sd = r.SadasnjaVrednost;
                                    var span = t.Span(sd.ToString("N2"))
                                         .FontColor(sd > 0 ? Colors.Green.Darken2 : Colors.Grey.Darken1);
                                    if (sd > 0) span.Bold();
                                });

                                static IContainer RowStyle(IContainer c)
                                    => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                        .PaddingVertical(3).PaddingHorizontal(2)
                                        .DefaultTextStyle(x => x.FontSize(7.5f));
                            }

                            // Zbir za Amortizacionu Grupu
                            table.Cell().ColumnSpan(3).Element(SumStyle).Text($"Zbir za am. grupu {amGroup.Key}").Bold();
                            table.Cell().Element(SumStyle).AlignRight().Text(amGroup.Sum(x => x.NabavnaVrednost).ToString("N2")).Bold();
                            table.Cell().Element(SumStyle).AlignRight().Text(amGroup.Sum(x => x.PrethodnaIspravka).ToString("N2")).Bold();
                            table.Cell().Element(SumStyle).AlignRight().Text(amGroup.Sum(x => x.NovaAmortizacija).ToString("N2")).Bold().FontColor(Colors.Orange.Darken2);
                            table.Cell().Element(SumStyle).AlignRight().Text(amGroup.Sum(x => x.NovaIspravkaUkupno).ToString("N2")).Bold();
                            table.Cell().Element(SumStyle).AlignRight().Text(amGroup.Sum(x => x.SadasnjaVrednost).ToString("N2")).Bold().FontColor(Colors.Green.Darken2);
                        }

                        // Zbir za Konto
                        table.Cell().ColumnSpan(3).Element(KontoSumStyle).Text($"Zbir za konto {kontoGroup.Key}").Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoGroup.Sum(x => x.NabavnaVrednost).ToString("N2")).Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoGroup.Sum(x => x.PrethodnaIspravka).ToString("N2")).Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoGroup.Sum(x => x.NovaAmortizacija).ToString("N2")).Bold().FontColor(Colors.Orange.Darken2);
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoGroup.Sum(x => x.NovaIspravkaUkupno).ToString("N2")).Bold();
                        table.Cell().Element(KontoSumStyle).AlignRight().Text(kontoGroup.Sum(x => x.SadasnjaVrednost).ToString("N2")).Bold().FontColor(Colors.Green.Darken2);
                    });
                }

                // Zbir za OJ
                col.Item().PaddingTop(4).Table(ojSumTable =>
                {
                    ojSumTable.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(50);
                        c.RelativeColumn();
                        c.ConstantColumn(35);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                        c.ConstantColumn(65);
                    });

                    ojSumTable.Cell().ColumnSpan(3).Element(OjSumStyle).Text($"Zbir za obracunsku jedinicu {ojGroup.Key}").Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojGroup.Sum(x => x.NabavnaVrednost).ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojGroup.Sum(x => x.PrethodnaIspravka).ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojGroup.Sum(x => x.NovaAmortizacija).ToString("N2")).Bold().FontColor(Colors.Orange.Darken2);
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojGroup.Sum(x => x.NovaIspravkaUkupno).ToString("N2")).Bold();
                    ojSumTable.Cell().Element(OjSumStyle).AlignRight().Text(ojGroup.Sum(x => x.SadasnjaVrednost).ToString("N2")).Bold().FontColor(Colors.Green.Darken2);
                });
            }

            // Ukupni zbir (kao "UKUPNI ZBIR" u Clipper-u)
            var ukNabavna = _rezultati.Sum(r => r.NabavnaVrednost);
            var ukPrethodna = _rezultati.Sum(r => r.PrethodnaIspravka);
            var ukNova = _rezultati.Sum(r => r.NovaAmortizacija);
            var ukUkupno = _rezultati.Sum(r => r.NovaIspravkaUkupno);
            var ukSadasnja = _rezultati.Sum(r => r.SadasnjaVrednost);

            col.Item().PaddingTop(15).Table(sumTable =>
            {
                sumTable.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(50);
                    c.RelativeColumn();
                    c.ConstantColumn(35);
                    c.ConstantColumn(65);
                    c.ConstantColumn(65);
                    c.ConstantColumn(65);
                    c.ConstantColumn(65);
                    c.ConstantColumn(65);
                });

                sumTable.Cell().ColumnSpan(3).Element(SumStyle).Text("UKUPNI ZBIR SVIH SREDSTAVA").Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukNabavna.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukPrethodna.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(t =>
                    t.Span(ukNova.ToString("N2")).Bold().FontColor(Colors.Orange.Darken2));
                sumTable.Cell().Element(SumStyle).AlignRight().Text(ukUkupno.ToString("N2")).Bold();
                sumTable.Cell().Element(SumStyle).AlignRight().Text(t =>
                    t.Span(ukSadasnja.ToString("N2")).Bold().FontColor(Colors.Green.Darken2));

                static IContainer SumStyle(IContainer c)
                    => c.Background(Colors.Indigo.Lighten5)
                        .BorderTop(2).BorderColor(Colors.Indigo.Darken4)
                        .BorderBottom(2).BorderColor(Colors.Indigo.Darken4)
                        .PaddingVertical(6).PaddingHorizontal(4)
                        .DefaultTextStyle(x => x.FontSize(10f));
            });

            static IContainer SumStyle(IContainer c)
                => c.Background(Colors.Grey.Lighten4)
                    .BorderTop(1).BorderColor(Colors.Grey.Darken1)
                    .PaddingVertical(4).PaddingHorizontal(4)
                    .DefaultTextStyle(x => x.FontSize(8.5f));

            static IContainer KontoSumStyle(IContainer c)
                => c.Background(Colors.Indigo.Lighten5)
                    .BorderTop(1).BorderColor(Colors.Indigo.Darken2)
                    .PaddingVertical(4).PaddingHorizontal(4)
                    .DefaultTextStyle(x => x.FontSize(9f));

            static IContainer OjSumStyle(IContainer c)
                => c.Background(Colors.Indigo.Lighten4)
                    .BorderTop(1).BorderColor(Colors.Indigo.Darken3)
                    .PaddingVertical(5).PaddingHorizontal(4)
                    .DefaultTextStyle(x => x.FontSize(9.5f));
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
