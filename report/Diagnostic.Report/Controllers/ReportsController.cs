using System.Security.Claims;
using System.Text;
using Diagnostic.Report.Data;
using Diagnostic.Report.Documents;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Diagnostic.Report.Controllers;

[ApiController, Route("api/reports")]
public class ReportsController(ReportRepository repo, IConfiguration config) : ControllerBase
{
    private bool SuperAdmin => User.FindFirstValue("sa") == "1";
    private List<int> RoleIds => User.FindAll("rid").Select(c => int.Parse(c.Value)).ToList();
    private bool DefaultShowLetterHead => config.GetValue("Report:ShowLetterHead", true);

    /// <summary>Reports (with their parameters) the user may run.</summary>
    [HttpGet]
    public Task<List<ReportDefinition>> List() => repo.ListAsync(SuperAdmin, RoleIds);

    /// <summary>
    /// Lab report of an invoice: all finalized tests, or only <paramref name="detailIds"/>.
    /// <paramref name="preview"/> also includes verified (not yet finalized) tests, watermarked "PRELIMINARY".
    /// </summary>
    [HttpGet("lab-report/{investigationId:int}")]
    public async Task<IActionResult> LabReport(int investigationId, [FromQuery] string? detailIds, [FromQuery] bool? letterHead, [FromQuery] int? letterHeadId,
        [FromQuery] bool preview = false)
    {
        var def = await repo.GetAsync("LAB_REPORT", SuperAdmin, RoleIds);
        var data = await repo.LabReportAsync(def, investigationId, detailIds, letterHeadId, preview);
        var pdf = new LabReportDocument(data, letterHead ?? DefaultShowLetterHead).GeneratePdf();
        if (!preview) await repo.MarkPrintedAsync(data.Tests.Select(t => t.DetailId));
        return Pdf(pdf, $"LabReport-{data.Header.InvoiceNo}");
    }

    [HttpGet("money-receipt/{investigationId:int}")]
    public async Task<IActionResult> MoneyReceipt(int investigationId, [FromQuery] int? paymentId, [FromQuery] bool? letterHead, [FromQuery] int? letterHeadId)
    {
        var def = await repo.GetAsync("MONEY_RECEIPT", SuperAdmin, RoleIds);
        var data = await repo.MoneyReceiptAsync(def, investigationId, paymentId, letterHeadId);
        return Pdf(new MoneyReceiptDocument(data, letterHead ?? true).GeneratePdf(), $"Receipt-{data.Header.InvoiceNo}");
    }

    /// <summary>Barcode stickers for all samples of an invoice or for one sample.</summary>
    [HttpGet("sticker")]
    public async Task<IActionResult> Sticker([FromQuery] int? investigationId, [FromQuery] int? sampleId, [FromQuery] string? sampleIds,
        [FromQuery] float width = 50, [FromQuery] float height = 25)
    {
        var def = await repo.GetAsync("PATHOLOGY_STICKER", SuperAdmin, RoleIds);
        var rows = new List<StickerRow>();
        var ids = (sampleIds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.TryParse(s, out var v) ? v : 0).Where(v => v > 0).Distinct().Take(200).ToList();
        if (ids.Count > 0)
            foreach (var id in ids) rows.AddRange(await repo.StickersAsync(def, null, id));
        else
            rows = await repo.StickersAsync(def, investigationId, sampleId);
        if (rows.Count == 0) return NotFound(new { message = "No collected sample found." });
        return Pdf(new StickerDocument(rows, width, height).GeneratePdf(), $"Sticker-{rows[0].InvoiceNo}");
    }

    /// <summary>
    /// Runs any report from "Reports and Sp Mapping" by its code. Parameters come from the query string
    /// (e.g. <c>?FromDate=2026-09-01&amp;ToDate=2026-09-30</c>). <c>format=csv</c> exports the data (needs export permission).
    /// </summary>
    [HttpGet("run/{code}")]
    public async Task<IActionResult> Run(string code, [FromQuery] string format = "pdf")
    {
        var csv = format.Equals("csv", StringComparison.OrdinalIgnoreCase);
        var excel = format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) || format.Equals("excel", StringComparison.OrdinalIgnoreCase);
        var def = await repo.GetAsync(code, SuperAdmin, RoleIds, export: csv || excel);

        switch (def.Template)
        {
            case "LabReport":
                return await LabReport(QueryInt("InvestigationId") ?? throw new ReportException("InvestigationId is required."),
                    Request.Query["DetailIds"], QueryBool("letterHead"), QueryInt("letterHeadId"), QueryBool("preview") ?? false);
            case "MoneyReceipt":
                return await MoneyReceipt(QueryInt("InvestigationId") ?? throw new ReportException("InvestigationId is required."),
                    QueryInt("PaymentId"), QueryBool("letterHead"), QueryInt("letterHeadId"));
            case "Sticker":
                return await Sticker(QueryInt("InvestigationId"), QueryInt("SampleId"), Request.Query["SampleIds"]);
        }

        var data = await repo.TabularAsync(def, Request.Query);
        if (csv) return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(ToCsv(data))).ToArray(), "text/csv", $"{def.Code}.csv");
        if (excel)
            return File(ExcelExport.Build(data), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{def.Code}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx");

        var lh = await repo.LetterHeadAsync(null, null);
        var pdf = new TabularDocument(data, lh, def.Orientation == "Landscape").GeneratePdf();
        return Pdf(pdf, def.Code);
    }

    private FileContentResult Pdf(byte[] bytes, string name)
    {
        Response.Headers.ContentDisposition = $"inline; filename=\"{name}.pdf\"";
        return File(bytes, "application/pdf");
    }

    private int? QueryInt(string key) => int.TryParse(Request.Query[key], out var v) ? v : null;
    private bool? QueryBool(string key) => bool.TryParse(Request.Query[key], out var v) ? v : null;

    private static string ToCsv(TabularData d)
    {
        static string Esc(string s) => s.Contains(',') || s.Contains('"') || s.Contains('\n') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', d.Columns.Select(Esc)));
        foreach (var r in d.Rows) sb.AppendLine(string.Join(',', r.Select(v => Esc(v is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm") : v?.ToString() ?? ""))));
        return sb.ToString();
    }
}
