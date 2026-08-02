using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERPiSredstvaData.Models;
using ERPiSredstvaData.Services;

namespace ERPiSredstvaApp.Views.Amortizacija;

public class ObrazacOADocument : IDocument
{
    private readonly List<PoreskaAmortizacijaCalculator.RezultatPoreskeAmortizacije> _rezultati;
    private readonly Firma? _firma;
    private readonly int _godina;
    private readonly string _primaryColor = "#1E3A8A";

    public ObrazacOADocument(
        List<PoreskaAmortizacijaCalculator.RezultatPoreskeAmortizacije> rezultati,
        Firma? firma,
        int godina)
    {
        _rezultati = rezultati;
        _firma = firma;
        _godina = godina;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
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
                column.Item().Text("OBRAZAC OA").FontSize(14).Bold().FontColor(_primaryColor);
                column.Item().Text("Obračun poreske amortizacije za sredstva nabavljena od 1. januara 2019. godine").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken3);
                column.Item().Text($"Za godinu: {_godina}.").FontSize(9).FontColor(Colors.Grey.Darken2);
            });

            row.ConstantItem(250).AlignRight().Column(column =>
            {
                if (_firma != null)
                {
                    column.Item().AlignRight().Text(_firma.Naziv).FontSize(11).Bold().FontColor(Colors.Black);
                    if (!string.IsNullOrEmpty(_firma.Mesto))
                        column.Item().AlignRight().Text(_firma.Mesto).FontSize(9).FontColor(Colors.Grey.Darken2);
                    if (!string.IsNullOrEmpty(_firma.PIB))
                        column.Item().AlignRight().Text($"PIB: {_firma.PIB} | MB: {_firma.MaticniBroj}").FontSize(9).FontColor(Colors.Grey.Darken2);
                }
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25);  // R.br
                columns.ConstantColumn(50);  // Šifra
                columns.ConstantColumn(65);  // Inv. Broj
                columns.RelativeColumn(3);  // Naziv Sredstva
                columns.ConstantColumn(60);  // Datum Akt.
                columns.ConstantColumn(50);  // Gr / Stopa
                columns.ConstantColumn(70);  // Poreska Osnovica
                columns.ConstantColumn(65);  // Preth. Ispravka
                columns.ConstantColumn(75);  // Poreska Amortizacija
                columns.ConstantColumn(70);  // Neotpisana Vr.
                columns.ConstantColumn(75);  // Razlika (PB-1)
            });

            // Header Row
            table.Header(header =>
            {
                static IContainer HeaderStyle(IContainer c) => c
                    .Background("#1E3A8A")
                    .Padding(4)
                    .AlignMiddle();

                header.Cell().Element(HeaderStyle).Text("R.br").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).Text("Šifra").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).Text("Inv. Br.").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).Text("Naziv osnovnog sredstva").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).AlignRight().Text("Aktivirano").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).AlignRight().Text("Stopa %").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).AlignRight().Text("Poreska Osnovica").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).AlignRight().Text("Preth. Ispravka").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).AlignRight().Text("Poreska Amort.").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).AlignRight().Text("Neotpisano").Bold().FontColor(Colors.White);
                header.Cell().Element(HeaderStyle).AlignRight().Text("Razlika (PB-1)").Bold().FontColor(Colors.White);
            });

            int rbr = 1;
            foreach (var r in _rezultati.OrderBy(x => x.LegacySifra))
            {
                bool alt = rbr % 2 == 0;
                string bg = alt ? "#F8FAFC" : "#FFFFFF";

                IContainer CellStyle(IContainer c) => c
                    .Background(bg)
                    .BorderBottom(0.5f)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(3)
                    .AlignMiddle();

                table.Cell().Element(CellStyle).Text(rbr.ToString());
                table.Cell().Element(CellStyle).Text(r.LegacySifra > 0 ? r.LegacySifra.ToString() : "");
                table.Cell().Element(CellStyle).Text(r.InventarskiBroj);
                table.Cell().Element(CellStyle).Text(r.Naziv);
                table.Cell().Element(CellStyle).AlignRight().Text(r.DatumAktiviranja.ToString("dd.MM.yyyy"));
                table.Cell().Element(CellStyle).AlignRight().Text($"{r.PoreskaStopa:N2}%");
                table.Cell().Element(CellStyle).AlignRight().Text(r.PoreskaNabavnaVrednost.ToString("N2"));
                table.Cell().Element(CellStyle).AlignRight().Text(r.PrethodnaPoreskaIspravka.ToString("N2"));
                table.Cell().Element(CellStyle).AlignRight().Text(r.NovaPoreskaAmortizacija.ToString("N2")).Bold().FontColor(Colors.Blue.Medium);
                table.Cell().Element(CellStyle).AlignRight().Text(r.PoreskaNeotpisanaVrednost.ToString("N2"));
                table.Cell().Element(CellStyle).AlignRight().Text(r.PrivremenaPoreskaRazlika.ToString("N2")).FontColor(r.PrivremenaPoreskaRazlika != 0 ? Colors.Orange.Darken2 : Colors.Grey.Darken1);

                rbr++;
            }

            // Total Summary Row
            static IContainer SumStyle(IContainer c) => c
                .Background("#E2E8F0")
                .BorderTop(1)
                .BorderColor(Colors.Grey.Darken1)
                .Padding(4)
                .AlignMiddle();

            table.Cell().ColumnSpan(6).Element(SumStyle).Text("UKUPNO (OBRAZAC OA):").Bold();
            table.Cell().Element(SumStyle).AlignRight().Text(_rezultati.Sum(x => x.PoreskaNabavnaVrednost).ToString("N2")).Bold();
            table.Cell().Element(SumStyle).AlignRight().Text(_rezultati.Sum(x => x.PrethodnaPoreskaIspravka).ToString("N2")).Bold();
            table.Cell().Element(SumStyle).AlignRight().Text(_rezultati.Sum(x => x.NovaPoreskaAmortizacija).ToString("N2")).Bold().FontColor(Colors.Blue.Darken2);
            table.Cell().Element(SumStyle).AlignRight().Text(_rezultati.Sum(x => x.PoreskaNeotpisanaVrednost).ToString("N2")).Bold();
            table.Cell().Element(SumStyle).AlignRight().Text(_rezultati.Sum(x => x.PrivremenaPoreskaRazlika).ToString("N2")).Bold().FontColor(Colors.Orange.Darken3);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(x =>
            {
                x.Span("Dokument generisan u aplikaciji ERPiSredstvaApp — Obrazac OA (Zakon o porezu na dobit pravnih lica)").FontSize(8).FontColor(Colors.Grey.Darken1);
            });
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Stranica ");
                x.CurrentPageNumber();
                x.Span(" od ");
                x.TotalPages();
            });
        });
    }
}
