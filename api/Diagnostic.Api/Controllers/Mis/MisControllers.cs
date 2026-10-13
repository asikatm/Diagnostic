using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers.Mis;

public record MediaCommissionSummaryRow(int MediaId, string? Code, string Media, string? Phone, int Invoices, int Tests,
    decimal BillAmount, decimal Commission);

public record MediaCommissionDetailRow(int MediaId, string Media, int InvestigationId, string InvoiceNo, DateTime InvoiceDate,
    string PatientName, string TestName, string GroupName, decimal BillAmount, decimal Commission, decimal InvoiceDue);

public record MediaCommissionResult(DateTime From, DateTime To, List<MediaCommissionSummaryRow> Summary, List<MediaCommissionDetailRow> Details,
    int TotalInvoices, int TotalTests, decimal TotalBill, decimal TotalCommission);

/// <summary>Dropdown data for the MIS report filter panel (Common / Diagnostic / Marketing sections).</summary>
[ApiController, Route("api/mis/filters"), MenuCode(MenuCodes.DiagReports)]
public class MisFiltersController(DiagnosticDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(new
    {
        tenants = await db.Tenants.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name, x.Code)).ToListAsync(),
        doctors = await db.Doctors.AsNoTracking().Where(x => x.IsReferrer).OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name, x.Code)).ToListAsync(),
        users = await db.Users.AsNoTracking().OrderBy(x => x.FullName).Select(x => new LookupItem(x.Id, x.FullName + " (" + x.UserName + ")", x.UserName)).ToListAsync(),
        parentGroups = await db.ParentGroups.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new LookupItem(x.Id, x.Name, x.Code)).ToListAsync(),
        testGroups = await db.TestGroups.AsNoTracking().OrderBy(x => x.SortOrder)
            .Select(x => new { x.Id, x.Name, x.Code, x.ParentGroupId }).ToListAsync(),
        testItems = await db.TestItems.AsNoTracking().OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Code, x.TestGroupId, x.TestGroup!.ParentGroupId }).ToListAsync(),
        paymentModes = await db.PaymentModes.AsNoTracking().OrderBy(x => x.SortOrder).Select(x => new LookupItem(x.Id, x.Name, x.Code)).ToListAsync(),
        medias = await db.Medias.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name, x.Code)).ToListAsync(),
        discountSetups = await db.DiscountSetups.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name, x.Code)).ToListAsync(),
        packages = await db.OpdPackages.AsNoTracking().OrderBy(x => x.Name).Select(x => new LookupItem(x.Id, x.Name, x.Code)).ToListAsync(),
    });
}

/// <summary>MIS → Media Commission: commission earned by every media (or one media) in a date range.</summary>
[ApiController, Route("api/mis/media-commission"), MenuCode(MenuCodes.MisMediaCommission)]
public class MisMediaCommissionController(DiagnosticDbContext db) : ControllerBase
{
    /// <param name="mediaId">Empty = all media.</param>
    /// <param name="includeZero">Also list active media without business in the period.</param>
    [HttpGet]
    public async Task<MediaCommissionResult> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? mediaId,
        [FromQuery] bool includeZero = true)
    {
        var start = (from ?? DateTime.Today).Date;
        var end = (to ?? DateTime.Today).Date;
        if (end < start) throw new AppException("'To' date cannot be before 'From' date.");

        // Billed, non-cancelled tests of invoices that came through a media.
        var details = await db.InvestigationDetails.AsNoTracking()
            .Where(d => d.Status != TestStatus.Cancelled && d.Investigation!.MediaId != null
                        && d.Investigation.InvoiceDate >= start && d.Investigation.InvoiceDate < end.AddDays(1)
                        && (mediaId == null || d.Investigation.MediaId == mediaId))
            .OrderBy(d => d.Investigation!.Media!.Name).ThenBy(d => d.Investigation!.InvoiceDate).ThenBy(d => d.Id)
            .Select(d => new MediaCommissionDetailRow(d.Investigation!.MediaId!.Value, d.Investigation.Media!.Name, d.InvestigationId,
                d.Investigation.InvoiceNo, d.Investigation.InvoiceDate, d.Investigation.Patient!.Name, d.TestItem!.Name,
                d.TestItem.TestGroup!.Name, d.UnitPrice - d.DiscountAmount, d.MediaCommission, d.Investigation.DueAmount))
            .ToListAsync();

        var summary = details.GroupBy(d => d.MediaId).ToDictionary(g => g.Key, g => g.ToList());
        var withBusiness = summary.Keys.ToList();
        var medias = await db.Medias.AsNoTracking()
            .Where(m => (mediaId == null || m.Id == mediaId) && ((includeZero && m.IsActive) || withBusiness.Contains(m.Id)))
            .Select(m => new { m.Id, m.Code, m.Name, m.Phone }).ToListAsync();

        var rows = medias.Select(m =>
            {
                var lines = summary.GetValueOrDefault(m.Id) ?? [];
                return new MediaCommissionSummaryRow(m.Id, m.Code, m.Name, m.Phone, lines.Select(l => l.InvestigationId).Distinct().Count(),
                    lines.Count, lines.Sum(l => l.BillAmount), lines.Sum(l => l.Commission));
            })
            .OrderByDescending(r => r.Commission).ThenBy(r => r.Media).ToList();

        return new MediaCommissionResult(start, end, rows, details,
            details.Select(d => d.InvestigationId).Distinct().Count(), details.Count, details.Sum(d => d.BillAmount), details.Sum(d => d.Commission));
    }

    /// <summary>Media list for the filter dropdown.</summary>
    [HttpGet("medias")]
    public async Task<List<LookupItem>> Medias() =>
        await db.Medias.AsNoTracking().OrderBy(m => m.Name).Select(m => new LookupItem(m.Id, m.Name, m.Code)).ToListAsync();
}
