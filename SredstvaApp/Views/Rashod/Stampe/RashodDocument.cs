using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SredstvaData.Models;

namespace SredstvaApp.Views.Rashod.Stampe;

public class RashodStavkaInfo
{
    public string Sifra { get; set; } = string.Empty;
    public string NazivSredstva { get; set; } = string.Empty;
    public string OpisPromene { get; set; } = string.Empty;
    public decimal Podaci { get; set; }
    public int ObracunskaJedinica { get; set; }
    public DateTime Datum { get; set; }
    public string DokumentBroj { get; set; } = string.Empty;
}

public class RashodNalogInfo
{
    public int BrojNaloga { get; set; }
    public List<RashodStavkaInfo> Stavke { get; set; } = new();
}

public class RashodDocument : IDocument
{
    private readonly List<RashodNalogInfo> _nalozi;
    private readonly string _nazivFirme;

    public RashodDocument(List<RashodNalogInfo> nalozi, string nazivFirme)
    {
        _nalozi = nalozi;
        _nazivFirme = nazivFirme;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
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
                col.Item().Text(_nazivFirme).Bold().FontSize(12);
                col.Item().PaddingTop(8).Text("PROMENE OSNOVNIH SREDSTAVA").Bold().FontSize(16);
            });
            row.ConstantItem(180).AlignRight().Column(col =>
            {
                col.Item().Text($"Datum štampe: {DateTime.Now:dd.MM.yyyy.}").FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().Text($"Broj naloga: {_nalozi.Count}").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(8).Column(col =>
        {
            foreach (var nalog in _nalozi)
            {
                // Naslov naloga
                col.Item().PaddingTop(12).Background(Colors.Indigo.Lighten5).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text($"NALOG BR. {nalog.BrojNaloga}").Bold().FontSize(11).FontColor(Colors.Indigo.Darken3);
                    r.ConstantItem(200).AlignRight().Text($"Stavki: {nalog.Stavke.Count}").FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                // Tabela
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(70);  // Šifra
                        columns.RelativeColumn();    // Naziv
                        columns.ConstantColumn(100); // Opis promene
                        columns.ConstantColumn(70);  // Podaci
                        columns.ConstantColumn(30);  // OJ
                        columns.ConstantColumn(60);  // Datum
                        columns.ConstantColumn(60);  // Dokument
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("Šifra").Bold();
                        header.Cell().Element(HeaderStyle).Text("Naziv sredstva").Bold();
                        header.Cell().Element(HeaderStyle).Text("Opis promene").Bold();
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Podaci").Bold();
                        header.Cell().Element(HeaderStyle).Text("OJ").Bold();
                        header.Cell().Element(HeaderStyle).Text("Datum").Bold();
                        header.Cell().Element(HeaderStyle).Text("Dokument").Bold();

                        static IContainer HeaderStyle(IContainer c)
                            => c.Background(Colors.Indigo.Darken4)
                                .PaddingVertical(4).PaddingHorizontal(4)
                                .DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(8f));
                    });

                    // Redovi
                    foreach (var s in nalog.Stavke)
                    {
                        table.Cell().Element(RowStyle).Text(s.Sifra);
                        table.Cell().Element(RowStyle).Text(s.NazivSredstva);
                        table.Cell().Element(RowStyle).Text(s.OpisPromene);
                        table.Cell().Element(RowStyle).AlignRight().Text(s.Podaci.ToString("N2"));
                        table.Cell().Element(RowStyle).Text(s.ObracunskaJedinica.ToString());
                        table.Cell().Element(RowStyle).Text(s.Datum.ToString("dd.MM.yyyy."));
                        table.Cell().Element(RowStyle).Text(s.DokumentBroj);

                        static IContainer RowStyle(IContainer c)
                            => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                .PaddingVertical(4).PaddingHorizontal(4)
                                .DefaultTextStyle(x => x.FontSize(7.5f));
                    }
                });

                // Suma naloga
                var ukupno = nalog.Stavke.Sum(s => s.Podaci);
                col.Item().AlignRight().PaddingRight(4).PaddingTop(2)
                    .Text($"Ukupno: {ukupno:N2}").Bold().FontSize(9).FontColor(Colors.Indigo.Darken3);
            }
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
