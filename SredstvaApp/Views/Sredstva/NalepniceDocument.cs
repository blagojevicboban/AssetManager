using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SredstvaData.Models;
using ZXing;
using ZXing.Common;

namespace SredstvaApp.Views.Sredstva;

public class NalepniceDocument : IDocument
{
    private readonly List<Sredstvo> _sredstva;
    private readonly Firma? _firma;
    private readonly BarcodeWriterSvg _barcodeWriter;

    public NalepniceDocument(List<Sredstvo> sredstva, Firma? firma)
    {
        _sredstva = sredstva;
        _firma = firma;
        
        _barcodeWriter = new BarcodeWriterSvg
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = 200,
                Height = 60,
                Margin = 0,
                PureBarcode = true
            }
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
                    
                    // Razmak
                    column.Item().PaddingTop(5);
                    
                    // Sifra i Inventarski broj
                    column.Item().AlignCenter().Text($"Šifra: {sredstvo.LegacySifra} | Inv.Br: {sredstvo.InventarskiBroj}").FontSize(8).Bold();
                    
                    // Razmak
                    column.Item().PaddingTop(5);
                    
                    // Bar kod
                    var barcodeContent = sredstvo.InventarskiBroj;
                    if (string.IsNullOrWhiteSpace(barcodeContent)) barcodeContent = sredstvo.LegacySifra.ToString();
                    
                    try 
                    {
                        var svgImage = _barcodeWriter.Write(barcodeContent);
                        // Fixing width and height so it scales nicely inside and doesn't push the column layout
                        column.Item().Width(150).Height(40).AlignCenter().Svg(svgImage.Content);
                    }
                    catch
                    {
                        column.Item().Width(150).Height(40).AlignCenter().AlignMiddle().Text("Nevažeći bar-kod").FontColor(Colors.Red.Medium);
                    }
                    
                    // Razmak
                    column.Item().PaddingTop(5);
                    
                    // Naziv sredstva (skraćeno ako je predugačko)
                    column.Item().AlignCenter().Text(sredstvo.Naziv).FontSize(8);
                });
            }
        });
    }
}
