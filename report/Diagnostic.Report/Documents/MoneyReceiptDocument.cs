using Diagnostic.Report.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Diagnostic.Report.Documents;

/// <summary>Invoice + money receipt (A5 portrait).</summary>
public class MoneyReceiptDocument(MoneyReceiptData data, bool showLetterHead) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        var h = data.Header;
        container.Page(page =>
        {
            page.Size(PageSizes.A5);
            page.Margin(10, Unit.Millimetre);
            page.DefaultTextStyle(s => s.FontSize(9));

            page.Header().LetterHead(data.LetterHead, showLetterHead, h.ReceiptNo != null ? "MONEY RECEIPT" : "INVOICE");

            page.Content().PaddingTop(6).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(58); c.RelativeColumn(); });
                        t.InfoCell("Patient ID", h.PatientCode);
                        t.InfoCell("Name", h.PatientName);
                        t.InfoCell("Age / Sex", $"{h.PatientAge} / {h.Gender}");
                        t.InfoCell("Mobile", h.Phone);
                        t.InfoCell("Ref. By", h.ReferDoctor ?? "Self");
                    });
                    row.ConstantItem(12);
                    row.RelativeItem().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.ConstantColumn(58); c.RelativeColumn(); });
                        t.InfoCell("Invoice No", h.InvoiceNo);
                        t.InfoCell("Date", h.InvoiceDate.ToString("dd-MMM-yyyy hh:mm tt"));
                        if (h.ReceiptNo != null) t.InfoCell("Receipt No", h.ReceiptNo);
                        if (h.PackageName != null) t.InfoCell("Package", h.PackageName);
                        if (h.Status != "Active") t.InfoCell("Status", h.Status);
                    });
                });
                col.Item().PaddingTop(8).Element(c => c.Barcode(h.InvoiceNo));

                col.Item().PaddingTop(6).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(20); c.RelativeColumn(4); c.RelativeColumn(1.4f); c.RelativeColumn(1.3f); c.RelativeColumn(1.4f);
                    });
                    t.Header(hd =>
                    {
                        foreach (var (text, right) in new[] { ("#", false), ("Test Name", false), ("Price", true), ("Disc.", true), ("Amount", true) })
                        {
                            var cell = hd.Cell().Background(Theme.Shade).BorderBottom(0.5f).BorderColor(Theme.Line).Padding(3);
                            (right ? cell.AlignRight() : cell).Text(text).Bold().FontSize(8.5f);
                        }
                    });
                    foreach (var r in data.Tests)
                    {
                        var cancelled = r.Status == "Cancelled";
                        void Cell(string text, bool right = false)
                        {
                            var c = t.Cell().BorderBottom(0.3f).BorderColor(Theme.Line).Padding(3);
                            var txt = (right ? c.AlignRight() : c).Text(text).FontSize(8.5f);
                            if (cancelled) txt.Strikethrough().FontColor(Theme.Muted);
                        }
                        Cell(r.Sl.ToString());
                        Cell(r.TestName + (cancelled ? "  (Cancelled)" : r.DeliveryDateTime is { } d ? $"   [Delivery: {d:dd-MMM hh:mm tt}]" : ""));
                        Cell(CommonParts.Money(r.UnitPrice), true);
                        Cell(CommonParts.Money(r.DiscountAmount), true);
                        Cell(CommonParts.Money(r.NetAmount), true);
                    }
                });

                col.Item().PaddingTop(6).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        if (data.Payments.Count == 0) return;
                        c.Item().Text("Payments").Bold().FontSize(8.5f);
                        foreach (var p in data.Payments)
                            c.Item().Text($"{p.ReceiptNo}  {p.PaymentDate:dd-MMM hh:mm tt}  {p.PaymentType}  {p.PaymentMode}{(p.PaymentChannel is null ? "" : " / " + p.PaymentChannel)}  {CommonParts.Money(p.Amount)}")
                                .FontSize(7.5f).FontColor(Theme.Muted);
                    });
                    row.ConstantItem(150).Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(); c.ConstantColumn(65); });
                        void Line(string label, decimal v, bool bold = false)
                        {
                            t.Cell().PaddingVertical(1).AlignRight().Text(label).FontSize(8.5f);
                            var txt = t.Cell().PaddingVertical(1).AlignRight().Text(CommonParts.Money(v)).FontSize(8.5f);
                            if (bold) txt.Bold();
                        }
                        Line("Gross Total", h.GrossAmount);
                        Line("Discount (-)", h.DiscountAmount);
                        if (h.VatAmount > 0) Line("VAT (+)", h.VatAmount);
                        Line("Net Payable", h.NetAmount, true);
                        Line("Paid", h.PaidAmount);
                        if (h.RefundAmount > 0) Line("Refunded", h.RefundAmount);
                        Line(h.DueAmount < 0 ? "Payable to patient" : "Due", Math.Abs(h.DueAmount), true);
                    });
                });

                if (h.ReceiptNo != null && h.ReceiptAmount.HasValue)
                    col.Item().PaddingTop(8).Background(Theme.Shade).Padding(5)
                        .Text($"Received with thanks {CommonParts.Money(h.ReceiptAmount.Value)} Taka ({h.ReceiptType}) against invoice {h.InvoiceNo}.")
                        .FontSize(8.5f).Italic();

                col.Item().PaddingTop(20).Row(r =>
                {
                    r.RelativeItem().Text($"Prepared by: {h.PreparedBy}").FontSize(8);
                    r.RelativeItem().AlignRight().Text("Authorised Signature").FontSize(8);
                });
            });

            page.Footer().Footer("Please bring this receipt when collecting your reports.");
        });
    }
}

public static class BarcodeExtensions
{
    public static void Barcode(this IContainer container, string value) =>
        container.AlignLeft().Width(55, Unit.Millimetre).Column(c =>
        {
            c.Item().Height(9, Unit.Millimetre).Svg(Documents.Barcode.Code128Svg(value));
        });
}
