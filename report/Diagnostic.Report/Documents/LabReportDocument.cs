using Diagnostic.Report.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Diagnostic.Report.Documents;

/// <summary>Lab report (A4). Each report heading (test group) starts on its own page; letter head is optional.</summary>
public class LabReportDocument(LabReportData data, bool showLetterHead) : IDocument
{
    public void Compose(IDocumentContainer container)
    {
        foreach (var section in data.Tests.GroupBy(t => t.ReportHeading ?? t.GroupName ?? "LABORATORY REPORT"))
        {
            var tests = section.ToList();
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(15, Unit.Millimetre);
                page.MarginVertical(12, Unit.Millimetre);
                page.DefaultTextStyle(s => s.FontSize(9.5f));

                // Not yet finalized by the pathologist → clearly marked, never mistaken for a final report.
                if (tests.Any(t => t.TestStatus == "Verified"))
                    page.Foreground().AlignCenter().AlignMiddle().Rotate(-35)
                        .Text("PRELIMINARY – NOT FINALIZED").FontSize(44).Bold().FontColor("#40D32F2F"); // #AARRGGBB – translucent red

                page.Header().Column(c =>
                {
                    c.Item().LetterHead(data.LetterHead, showLetterHead, section.Key);
                    c.Item().PaddingTop(6).Element(PatientBox);
                });

                page.Content().PaddingTop(8).Column(col =>
                {
                    foreach (var test in tests)
                    {
                        col.Item().PaddingTop(6).Text(test.TestName).Bold().FontSize(10.5f).FontColor(Theme.Primary);
                        if (test.ResultType == "Parameter") col.Item().Element(c => ParameterTable(c, test));
                        else col.Item().PaddingTop(4).Text(test.ReportText ?? "").LineHeight(1.35f);

                        if (!string.IsNullOrWhiteSpace(test.Comments))
                            col.Item().PaddingTop(4).Text(t =>
                            {
                                t.Span("Comment: ").SemiBold();
                                t.Span(test.Comments);
                            });
                    }
                    col.Item().PaddingTop(4).AlignCenter().Text("--- End of Report ---").FontSize(8).FontColor(Theme.Muted);
                });

                page.Footer().Column(c =>
                {
                    c.Item().PaddingBottom(6).Element(x => Signatures(x, tests));
                    c.Item().Footer(showLetterHead ? data.LetterHead?.FooterText : null);
                });
            });
        }
    }

    private void PatientBox(IContainer c)
    {
        var h = data.Header;
        var verified = data.Tests.Max(t => t.VerifiedAt);
        c.Border(0.5f).BorderColor(Theme.Line).Padding(5).Row(row =>
        {
            row.RelativeItem().Table(t =>
            {
                t.ColumnsDefinition(x => { x.ConstantColumn(62); x.RelativeColumn(); });
                t.InfoCell("Patient Name", h.PatientName);
                t.InfoCell("Age / Sex", $"{h.PatientAge} / {h.Gender}");
                t.InfoCell("Patient ID", h.PatientCode);
                t.InfoCell("Referred By", h.ReferDoctor ?? "Self");
            });
            row.RelativeItem().Table(t =>
            {
                t.ColumnsDefinition(x => { x.ConstantColumn(62); x.RelativeColumn(); });
                t.InfoCell("Invoice No", h.InvoiceNo);
                t.InfoCell("Sample No", h.SampleNo);
                t.InfoCell("Collected", h.CollectedAt?.ToString("dd-MMM-yyyy hh:mm tt"));
                t.InfoCell("Reported", verified?.ToString("dd-MMM-yyyy hh:mm tt"));
            });
            row.ConstantItem(100).AlignRight().AlignMiddle().Column(b =>
            {
                b.Item().Height(24).Svg(Barcode.Code128Svg(h.InvoiceNo));
                b.Item().AlignCenter().Text(h.InvoiceNo).FontSize(7);
            });
        });
    }

    private void ParameterTable(IContainer c, LabTestRow test)
    {
        var rows = data.Results.Where(r => r.DetailId == test.DetailId).OrderBy(r => r.SortOrder).ToList();
        c.PaddingTop(3).Table(t =>
        {
            t.ColumnsDefinition(x => { x.RelativeColumn(3.2f); x.RelativeColumn(1.8f); x.RelativeColumn(1.4f); x.RelativeColumn(2.6f); });
            t.Header(hd =>
            {
                foreach (var text in new[] { "Test", "Result", "Unit", "Reference Value" })
                    hd.Cell().Background(Theme.Shade).BorderBottom(0.8f).BorderColor(Theme.Primary).Padding(3).Text(text).Bold().FontSize(9);
            });

            string? lastHeading = null;
            foreach (var r in rows)
            {
                if (!string.IsNullOrWhiteSpace(r.GroupHeading) && r.GroupHeading != lastHeading)
                    t.Cell().ColumnSpan(4).PaddingTop(5).PaddingBottom(1).Text(r.GroupHeading).SemiBold().FontSize(8.5f).Underline();
                lastHeading = r.GroupHeading;

                var abnormal = r.Flag is "High" or "Low";
                t.Cell().BorderBottom(0.3f).BorderColor(Theme.Line).Padding(3)
                    .PaddingLeft(string.IsNullOrWhiteSpace(r.GroupHeading) ? 3 : 10).Text(r.ParameterName);
                var result = t.Cell().BorderBottom(0.3f).BorderColor(Theme.Line).Padding(3)
                    .Text((r.ResultValue ?? "") + (r.Flag == "High" ? "  H" : r.Flag == "Low" ? "  L" : ""));
                if (abnormal) result.Bold();
                t.Cell().BorderBottom(0.3f).BorderColor(Theme.Line).Padding(3).Text(r.Unit ?? "").FontColor(Theme.Muted);
                t.Cell().BorderBottom(0.3f).BorderColor(Theme.Line).Padding(3).Text(r.NormalRange ?? "").FontSize(8.5f);
            }
        });
    }

    private static void Signatures(IContainer c, List<LabTestRow> tests)
    {
        var signers = tests.Where(t => t.SignDoctorName != null)
            .Select(t => (t.SignDoctorName, t.SignDoctorDegree, t.SignDoctorDesignation)).Distinct().ToList();
        var verifiedBy = tests.Select(t => t.VerifiedBy ?? t.EnteredBy).FirstOrDefault(x => x != null);
        var finalized = tests.Where(t => t.FinalizedBy != null).OrderByDescending(t => t.FinalizedAt).FirstOrDefault();

        c.PaddingTop(16).Row(row =>
        {
            row.RelativeItem().AlignBottom().Column(col =>
            {
                col.Item().Text("Prepared / Checked by").FontSize(8).FontColor(Theme.Muted);
                col.Item().Text(verifiedBy ?? "").FontSize(8.5f).SemiBold();
                if (finalized != null)
                    col.Item().PaddingTop(2).Text($"Finalized by {finalized.FinalizedBy} on {finalized.FinalizedAt:dd-MMM-yyyy hh:mm tt}")
                        .FontSize(7.5f).FontColor(Theme.Muted);
            });
            foreach (var s in signers)
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().AlignRight().Width(140).LineHorizontal(0.5f);
                    col.Item().AlignRight().Text(s.SignDoctorName!).Bold().FontSize(9);
                    if (s.SignDoctorDegree != null) col.Item().AlignRight().Text(s.SignDoctorDegree).FontSize(8);
                    if (s.SignDoctorDesignation != null) col.Item().AlignRight().Text(s.SignDoctorDesignation).FontSize(8);
                });
        });
    }
}
