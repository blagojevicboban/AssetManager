using System;
using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ERPiSredstvaData.Models;
using ERPiSredstvaData.Services;

namespace ERPiSredstvaApp.Views.Amortizacija;

public class ObrazacPB1Document : IDocument
{
    private readonly List<PoreskaAmortizacijaCalculator.RezultatPoreskeAmortizacije> _rezultati;
    private readonly Firma? _firma;
    private readonly int _godina;
    private readonly string _primaryColor = "#1E3A8A";

    public ObrazacPB1Document(
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
            page.Size(PageSizes.A4.Portrait());
            page.Margin(0.8f, Unit.Centimetre);
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
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("IZVEŠTAJ PRIVREMENIH PORESKIH RAZLIKA (ZA OBRAZAC PB-1)").FontSize(13).Bold().FontColor(_primaryColor);
                column.Item().Text($"Za godinu: {_godina}").FontSize(10).SemiBold();
                column.Item().Text($"Obveznik: {_firma?.Naziv ?? "Preduzeće"} | PIB: {_firma?.PIB ?? "-"}").FontSize(8).Italic();
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30); // R.Br
                columns.ConstantColumn(80); // Inv. Br
                columns.RelativeColumn(2.5f); // Naziv sredstva
                columns.ConstantColumn(50); // Poreska grupa
                columns.RelativeColumn(1f); // Racunovodstvena Amortizacija
                columns.RelativeColumn(1f); // Poreska Amortizacija
                columns.RelativeColumn(1f); // Privremena Poreska Razlika
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("#").Bold();
                header.Cell().Element(HeaderStyle).Text("Inv. Br.").Bold();
                header.Cell().Element(HeaderStyle).Text("Naziv osnovnog sredstva").Bold();
                header.Cell().Element(HeaderStyle).Text("Gr.").Bold();
                header.Cell().Element(HeaderStyle).AlignRight().Text("Računovodst. Am.").Bold();
                header.Cell().Element(HeaderStyle).AlignRight().Text("Poreska Am.").Bold();
                header.Cell().Element(HeaderStyle).AlignRight().Text("Razlika (PB-1)").Bold();
            });

            int rb = 1;
            foreach (var r in _rezultati.OrderBy(x => x.PoreskaGrupa).ThenBy(x => x.InventarskiBroj))
            {
                table.Cell().Element(RowStyle).Text(rb++.ToString());
                table.Cell().Element(RowStyle).Text(r.InventarskiBroj);
                table.Cell().Element(RowStyle).Text(r.Naziv);
                table.Cell().Element(RowStyle).Text(r.PoreskaGrupa);
                table.Cell().Element(RowStyle).AlignRight().Text($"{r.RacunovodstvenaAmortizacija:N2}");
                table.Cell().Element(RowStyle).AlignRight().Text($"{r.NovaPoreskaAmortizacija:N2}");
                table.Cell().Element(RowStyle).AlignRight().Text($"{r.PrivremenaPoreskaRazlika:N2}");
            }

            // Footer zbirovi
            decimal ukupnoRac = _rezultati.Sum(r => r.RacunovodstvenaAmortizacija);
            decimal ukupnoPor = _rezultati.Sum(r => r.NovaPoreskaAmortizacija);
            decimal ukupnoRaz = _rezultati.Sum(r => r.PrivremenaPoreskaRazlika);

            table.Cell().ColumnSpan(4).Element(FooterStyle).Text("UKUPNO ZA PORESKI BILANS (OBRAZAC PB-1):").Bold();
            table.Cell().Element(FooterStyle).AlignRight().Text($"{ukupnoRac:N2}").Bold();
            table.Cell().Element(FooterStyle).AlignRight().Text($"{ukupnoPor:N2}").Bold();
            table.Cell().Element(FooterStyle).AlignRight().Text($"{ukupnoRaz:N2}").Bold();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Izveštaj generisan: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(8).Italic();
            row.RelativeItem().AlignRight().Text(x =>
            {
                x.Span("Strana ");
                x.CurrentPageNumber();
                x.Span(" od ");
                x.TotalPages();
            });
        });
    }

    private static IContainer HeaderStyle(IContainer container)
    {
        return container
            .Background("#F1F5F9")
            .BorderBottom(1)
            .BorderColor("#CBD5E1")
            .PaddingVertical(4)
            .PaddingHorizontal(2);
    }

    private static IContainer RowStyle(IContainer container)
    {
        return container
            .BorderBottom(0.5f)
            .BorderColor("#E2E8F0")
            .PaddingVertical(3)
            .PaddingHorizontal(2);
    }

    private static IContainer FooterStyle(IContainer container)
    {
        return container
            .Background("#E2E8F0")
            .BorderTop(1)
            .BorderColor("#94A3B8")
            .PaddingVertical(4)
            .PaddingHorizontal(2);
    }
}
