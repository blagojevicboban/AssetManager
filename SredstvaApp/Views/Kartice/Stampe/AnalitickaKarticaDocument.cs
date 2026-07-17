using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SredstvaData.Models;

namespace SredstvaApp.Views.Kartice.Stampe;

public class AnalitickaKarticaDocument : IDocument
{
    private readonly Sredstvo _sredstvo;
    private readonly List<Kartica> _kartice;
    private readonly SredstvaData.Models.Firma? _firma;
    private readonly string _primaryColor = "#2B4B80";

    public AnalitickaKarticaDocument(Sredstvo sredstvo, List<Kartica> kartice, SredstvaData.Models.Firma? firma)
    {
        _sredstvo = sredstvo;
        _kartice = kartice.OrderBy(k => k.Datum).ThenBy(k => k.RedBroj).ToList();
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
                col.Item().PaddingTop(10).Text($"ANALITIČKA KARTICA OSNOVNOG SREDSTVA").SemiBold().FontSize(14).FontColor(_primaryColor);
                col.Item().Text($"Inventarski br: {_sredstvo.InventarskiBroj}  •  Šifra: {_sredstvo.LegacySifra}").FontSize(10).FontColor(Colors.Grey.Medium);
                col.Item().Text($"Naziv: {_sredstvo.Naziv}").SemiBold().FontSize(12).FontColor(_primaryColor);
            });

            row.ConstantItem(250).AlignRight().Column(column =>
            {
                if (_firma != null)
                {
                    column.Item().AlignRight().Text(_firma.Naziv).FontSize(12).SemiBold().FontColor(Colors.Black);
                    if (!string.IsNullOrEmpty(_firma.Mesto))
                        column.Item().AlignRight().Text(_firma.Mesto).FontSize(10).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrEmpty(_firma.PIB))
                        column.Item().AlignRight().Text($"PIB: {_firma.PIB}").FontSize(10).FontColor(Colors.Grey.Darken2);
                }
                column.Item().PaddingTop(5).AlignRight().Text($"Datum štampe: {DateTime.Now:dd.MM.yyyy.}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(6).Column(col =>
        {
            // Statistical boxes (4 columns)
            col.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(c =>
                {
                    c.Item().Text("NABAVNA VREDNOST").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(4).Text(_sredstvo.NabavnaVrednost.ToString("N2")).FontSize(14).Bold();
                    c.Item().PaddingTop(2).Text("RSD").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().PaddingLeft(12).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(c =>
                {
                    c.Item().Text("ISPRAVKA VREDNOSTI").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(4).Text(_sredstvo.IspravkaVrednosti.ToString("N2")).FontSize(14).Bold().FontColor(Colors.Orange.Medium);
                    c.Item().PaddingTop(2).Text("RSD").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().PaddingLeft(12).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(c =>
                {
                    c.Item().Text("SADAŠNJA VREDNOST").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(4).Text(_sredstvo.SadasnjaVrednost.ToString("N2")).FontSize(14).Bold().FontColor(Colors.Green.Medium);
                    c.Item().PaddingTop(2).Text("RSD").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().PaddingLeft(12).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(c =>
                {
                    c.Item().Text("STOPA AMORTIZACIJE").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                    c.Item().PaddingTop(4).Text($"{_sredstvo.StopaAmortizacije:N2} %").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                    c.Item().PaddingTop(2).Text("godišnje").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });

            col.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(6).Column(meta =>
            {
                meta.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("INVENTARSKI BROJ").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                        c.Item().PaddingTop(3).Text(_sredstvo.InventarskiBroj).FontSize(11);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("AMORT. GRUPA").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                        c.Item().PaddingTop(3).Text(_sredstvo.AmortizacionaGrupa).FontSize(11);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("DATUM AKTIVIRANJA").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                        c.Item().PaddingTop(3).Text(_sredstvo.DatumAktiviranja == DateTime.MinValue ? "—" : _sredstvo.DatumAktiviranja.ToString("dd.MM.yyyy")).FontSize(11);
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("BROJ STAVKI").FontSize(9).SemiBold().FontColor(Colors.Grey.Darken1);
                        c.Item().PaddingTop(3).Text(_kartice.Count.ToString()).FontSize(11);
                    });
                });
            });

            // History table
            col.Item().PaddingTop(10).Text("Hronologija promena").FontSize(12).SemiBold().FontColor(_primaryColor);

            col.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);  // Datum
                    columns.RelativeColumn(2);   // Opis promena
                    columns.ConstantColumn(40);  // Konto
                    columns.ConstantColumn(30);  // Am.Gr.
                    columns.ConstantColumn(35);  // Stopa%
                    columns.ConstantColumn(50);  // Nabavna V.
                    columns.ConstantColumn(50);  // Ispravka V.
                    columns.ConstantColumn(50);  // Sadašnja V.
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Datum").Bold();
                    header.Cell().Element(CellStyle).Text("Opis promena").Bold();
                    header.Cell().Element(CellStyle).Text("Konto").Bold();
                    header.Cell().Element(CellStyle).Text("Am. Gr.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Stopa%").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Nabavna V.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Ispravka V.").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Sadašnja V.").Bold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.Background(Colors.Indigo.Darken4)
                                        .PaddingVertical(4)
                                        .PaddingHorizontal(4)
                                        .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8f));
                    }
                });

                if (_kartice.Count == 0)
                {
                    table.Cell().ColumnSpan(8).Padding(10).AlignCenter().Text("Nema podataka").FontColor(Colors.Grey.Darken1);
                }
                else
                {
                    decimal kumulativnaNab = 0m;
                    decimal kumulativnaIsp = 0m;

                    foreach (var kartica in _kartice)
                    {
                        kumulativnaNab += kartica.NabavnaVrednost;
                        kumulativnaIsp += kartica.IspravkaVrednosti;

                        table.Cell().Element(CellStyle).Text(kartica.Datum.ToString("dd.MM.yyyy"));
                        table.Cell().Element(CellStyle).Text(kartica.OpisPromene);
                        table.Cell().Element(CellStyle).Text(kartica.Konto);
                        table.Cell().Element(CellStyle).Text($"{kartica.AmortizacionaGrupa1}");
                        table.Cell().Element(CellStyle).AlignRight().Text(kartica.StopaAmortizacije.ToString("N2"));
                        table.Cell().Element(CellStyle).AlignRight().Text(kumulativnaNab.ToString("N2"));
                        table.Cell().Element(CellStyle).AlignRight().Text(kumulativnaIsp.ToString("N2"));
                        table.Cell().Element(CellStyle).AlignRight().Text((kumulativnaNab - kumulativnaIsp).ToString("N2"));

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.BorderBottom(0.5f)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .PaddingVertical(4)
                                            .PaddingHorizontal(4)
                                            .DefaultTextStyle(x => x.FontSize(7.5f));
                        }
                    }
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
