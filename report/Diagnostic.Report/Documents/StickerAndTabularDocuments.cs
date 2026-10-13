using Diagnostic.Report.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Diagnostic.Report.Documents;

/// <summary>Pathology barcode sticker – one 50 x 25 mm label per sample tube.</summary>
public class StickerDocument(List<StickerRow> rows, float widthMm = 50, float heightMm = 25) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        foreach (var s in rows)
        {
            container.Page(page =>
            {
                page.Size(widthMm, heightMm, Unit.Millimetre);
                page.Margin(1.5f, Unit.Millimetre);
                page.DefaultTextStyle(t => t.FontSize(6.5f));
                page.Content().Column(c =>
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text(s.PatientName).Bold().FontSize(7).ClampLines(1);
                        r.AutoItem().Text($"{s.PatientAge} {s.Gender?[..1]}").FontSize(6.5f);
                    });
                    c.Item().Height(8.5f, Unit.Millimetre).Svg(Barcode.Code128Svg(s.SampleNo));
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Text(s.SampleNo).Bold().FontSize(7.5f);
                        r.AutoItem().Text(s.CollectedAt.ToString("dd/MM/yy HH:mm"));
                    });
                    c.Item().Row(r =>
                    {
                        if (!string.IsNullOrWhiteSpace(s.ColorHex))
                            r.ConstantItem(2.5f, Unit.Millimetre).Height(2.5f, Unit.Millimetre).Background(s.ColorHex!);
                        r.RelativeItem().PaddingLeft(1).Text($"{s.InvoiceNo}  {s.Vacutainer}").FontSize(6).ClampLines(1);
                    });
                    c.Item().Text(s.Tests ?? "").FontSize(6).ClampLines(1);
                });
            });
        }
    }
}

/// <summary>Generic grid for any stored procedure mapped as a "Tabular" report. Numeric columns are totalled.</summary>
public class TabularDocument(TabularData data, LetterHeadRow? letterHead, bool landscape) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        var numeric = data.Columns.Select((_, i) =>
            data.Rows.Count > 0 && data.Rows.All(r => r[i] is null or decimal or int or long or double or short or float)
            && data.Rows.Any(r => r[i] is not null)).ToArray();

        container.Page(page =>
        {
            page.Size(landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
            page.Margin(10, Unit.Millimetre);
            page.DefaultTextStyle(s => s.FontSize(8.5f));

            page.Header().Column(c =>
            {
                c.Item().LetterHead(letterHead, true, data.Title.ToUpperInvariant());
                if (data.Subtitle.Length > 0) c.Item().AlignCenter().Text(data.Subtitle).FontSize(8.5f).FontColor(Theme.Muted);
                c.Item().PaddingBottom(4);
            });

            // Column width follows its content: longest of header / values (numbers include the total), within limits.
            var widths = data.Columns.Select((c, i) =>
            {
                var longest = data.Rows.Select(r => Format(r[i]).Length).DefaultIfEmpty(0).Max();
                if (numeric[i]) longest = Math.Max(longest, Format(data.Rows.Sum(r => r[i] is null ? 0m : Convert.ToDecimal(r[i]))).Length);
                return (float)Math.Clamp(Math.Max(longest, c.Length * 0.9), 4, 30);
            }).ToArray();

            page.Content().Table(t =>
            {
                t.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(24);
                    foreach (var w in widths) cd.RelativeColumn(w);
                });
                t.Header(h =>
                {
                    h.Cell().Background(Theme.Primary).Padding(3).Text("SL").FontColor(Colors.White).Bold();
                    for (var i = 0; i < data.Columns.Count; i++)
                    {
                        var cell = h.Cell().Background(Theme.Primary).Padding(3);
                        (numeric[i] ? cell.AlignRight() : cell).Text(data.Columns[i]).FontColor(Colors.White).Bold();
                    }
                });

                var sl = 0;
                foreach (var row in data.Rows)
                {
                    var bg = ++sl % 2 == 0 ? Theme.Shade : "#FFFFFF";
                    t.Cell().Background(bg).Padding(2.5f).Text(sl.ToString());
                    for (var i = 0; i < row.Length; i++)
                    {
                        var cell = t.Cell().Background(bg).Padding(2.5f);
                        (numeric[i] ? cell.AlignRight() : cell).Text(Format(row[i]));
                    }
                }

                if (data.Rows.Count == 0)
                    t.Cell().ColumnSpan((uint)data.Columns.Count + 1).Padding(10).AlignCenter().Text("No data found for the selected criteria.").Italic();
                else if (numeric.Any(n => n))
                {
                    t.Cell().BorderTop(1).Padding(3).Text("");
                    for (var i = 0; i < data.Columns.Count; i++)
                    {
                        var cell = t.Cell().BorderTop(1).Padding(3);
                        if (numeric[i] && !data.Columns[i].Contains('%'))
                            cell.AlignRight().Text(Format(data.Rows.Sum(r => r[i] is null ? 0m : Convert.ToDecimal(r[i])))).Bold();
                        else cell.Text(i == 0 ? "Total" : "").Bold();
                    }
                }
            });

            page.Footer().Footer($"{data.Rows.Count} row(s)");
        });
    }

    public static string Format(object? v) => v switch
    {
        null => "",
        decimal d => d.ToString("#,##0.00"),
        double d => d.ToString("#,##0.00"),
        DateTime d => d.TimeOfDay == TimeSpan.Zero ? d.ToString("dd-MMM-yyyy") : d.ToString("dd-MMM-yyyy hh:mm tt"),
        _ => v.ToString() ?? "",
    };
}
