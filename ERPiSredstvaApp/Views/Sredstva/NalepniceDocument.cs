using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ERPiSredstvaData.Models;
using System.Collections.Generic;
using ZXing;
using ZXing.Common;
using ZXing.SkiaSharp;
using ZXing.SkiaSharp.Rendering;

namespace ERPiSredstvaApp.Views.Sredstva;

public class NalepniceDocument : IDocument
{
    private readonly List<Sredstvo> _sredstva;
    private readonly Firma? _firma;
    private readonly BarcodeWriter<SKBitmap> _barcodeWriter;

    public NalepniceDocument(List<Sredstvo> sredstva, Firma? firma)
    {
        _sredstva = sredstva;
        _firma = firma;

        _barcodeWriter = new BarcodeWriter<SKBitmap>
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = 200,
                Height = 60,
                Margin = 0,
                PureBarcode = true
            },
            Renderer = new SKBitmapRenderer()
        };
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Size(PageSizes.A4.Portrait());
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Content().Element(ComposeContent);
            });
    }

    private void ComposeContent(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
            });

            foreach (var sredstvo in _sredstva)
            {
                table.Cell().Padding(5).Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(column =>
                {
                    // Ime firme
                    column.Item().AlignCenter().Text(_firma?.Naziv ?? "Firma d.o.o.").FontSize(8).SemiBold();

                    column.Item().PaddingTop(5);

                    // Sifra i Inventarski broj
                    column.Item().AlignCenter().Text($"Šifra: {sredstvo.LegacySifra} | Inv.Br: {sredstvo.InventarskiBroj}").FontSize(8).Bold();

                    column.Item().PaddingTop(5);

                    // Bar kod
                    var barcodeContent = sredstvo.InventarskiBroj;
                    if (string.IsNullOrWhiteSpace(barcodeContent))
                        barcodeContent = sredstvo.LegacySifra.ToString();

                    var barcodePng = TryGenerateBarcodePng(barcodeContent);

                    if (barcodePng is not null)
                    {
                        column.Item().AlignCenter().Width(150).Height(40).Image(barcodePng);
                    }
                    else
                    {
                        column.Item().Width(150).Height(40).AlignCenter().AlignMiddle()
                            .Text("Nevažeći bar-kod").FontColor(Colors.Red.Medium);
                    }

                    column.Item().PaddingTop(5);

                    // Naziv sredstva
                    column.Item().AlignCenter().Text(sredstvo.Naziv).FontSize(8);
                });
            }
        });
    }

    private byte[]? TryGenerateBarcodePng(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var bitmap = _barcodeWriter.Write(content);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }
}