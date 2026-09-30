using System.Text;
using Diagnostic.Report.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ZXing;
using ZXing.OneD;

namespace Diagnostic.Report.Documents;

public static class Theme
{
    public const string Primary = "#0B4F71";
    public const string Muted = "#5F6B76";
    public const string Line = "#C9D3DC";
    public const string Shade = "#EEF3F7";
}

public static class Barcode
{
    /// <summary>Code-128 barcode as an SVG string (vector – prints sharp on any label printer).</summary>
    public static string Code128Svg(string value, int height = 40)
    {
        var matrix = new Code128Writer().encode(value, BarcodeFormat.CODE_128, 0, height);
        var width = matrix.Width;
        var sb = new StringBuilder($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {width} {height}\" preserveAspectRatio=\"none\">");
        for (var x = 0; x < width; x++)
        {
            if (!matrix[x, 0]) continue;
            var start = x;
            while (x + 1 < width && matrix[x + 1, 0]) x++;
            sb.Append($"<rect x=\"{start}\" y=\"0\" width=\"{x - start + 1}\" height=\"{height}\" fill=\"#000\"/>");
        }
        return sb.Append("</svg>").ToString();
    }
}

public static class CommonParts
{
    /// <summary>Letter head block. When <paramref name="show"/> is false, leaves blank space for pre-printed pads.</summary>
    public static void LetterHead(this IContainer container, LetterHeadRow? lh, bool show, string documentTitle)
    {
        container.Column(col =>
        {
            if (!show)
            {
                col.Item().Height((lh?.PrePrintedTopMarginMm ?? 45) - 15, Unit.Millimetre);
            }
            else if (lh != null)
            {
                col.Item().Row(row =>
                {
                    if (!string.IsNullOrWhiteSpace(lh.LogoPath) && File.Exists(lh.LogoPath))
                        row.ConstantItem(60).Height(50).Image(lh.LogoPath).FitArea();
                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().AlignCenter().Text(lh.Title ?? "").FontSize(17).Bold().FontColor(Theme.Primary);
                        if (!string.IsNullOrWhiteSpace(lh.SubTitle)) c.Item().AlignCenter().Text(lh.SubTitle).FontSize(10).Italic();
                        var contact = string.Join("  |  ", new[] { lh.Address, lh.Phone is null ? null : "Phone: " + lh.Phone, lh.Email }
                            .Where(s => !string.IsNullOrWhiteSpace(s)));
                        if (contact.Length > 0) c.Item().AlignCenter().Text(contact).FontSize(8.5f).FontColor(Theme.Muted);
                    });
                    if (!string.IsNullOrWhiteSpace(lh.LogoPath) && File.Exists(lh.LogoPath)) row.ConstantItem(60);
                });
                col.Item().PaddingTop(4).LineHorizontal(1.5f).LineColor(Theme.Primary);
            }
            col.Item().PaddingTop(6).AlignCenter().Text(documentTitle).FontSize(12).Bold().LetterSpacing(0.05f);
        });
    }

    public static void InfoCell(this TableDescriptor t, string label, string? value)
    {
        t.Cell().PaddingVertical(1.5f).Text(label).FontSize(8.5f).FontColor(Theme.Muted);
        t.Cell().PaddingVertical(1.5f).Text(": " + (value ?? "")).FontSize(9).SemiBold();
    }

    public static void Footer(this IContainer container, string? footerText)
    {
        container.Column(c =>
        {
            c.Item().LineHorizontal(0.5f).LineColor(Theme.Line);
            c.Item().PaddingTop(2).Row(r =>
            {
                r.RelativeItem().Text(footerText ?? "").FontSize(7.5f).FontColor(Theme.Muted);
                r.ConstantItem(170).AlignRight().Text(t =>
                {
                    t.DefaultTextStyle(s => s.FontSize(7.5f).FontColor(Theme.Muted));
                    t.Span($"Printed {DateTime.Now:dd-MMM-yyyy hh:mm tt}   Page ");
                    t.CurrentPageNumber();
                    t.Span(" of ");
                    t.TotalPages();
                });
            });
        });
    }

    public static string Money(decimal v) => v.ToString("#,##0.00");
}
