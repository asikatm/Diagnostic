using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Services;

public record DashboardSummary(
    int InvoiceCount, decimal GrossAmount, decimal DiscountAmount, decimal NetAmount, decimal Collection, decimal Refund,
    decimal TodayDue, decimal TotalDue, int PatientCount,
    int PendingSample, int PendingResult, int PendingVerify, int PendingFinalize, int ReadyToDeliver, int PendingCancel,
    List<DailyPoint> Last7Days, List<GroupPoint> TestGroups);

public record DailyPoint(DateTime Date, int Invoices, decimal NetAmount, decimal Collection);
public record GroupPoint(string GroupName, int Tests, decimal Amount);

public class DashboardService(DiagnosticDbContext db)
{
    public async Task<DashboardSummary> GetAsync(DateTime? from, DateTime? to)
    {
        var start = (from ?? DateTime.Today).Date;
        var end = (to ?? DateTime.Today).Date.AddDays(1);

        var inv = db.Investigations.AsNoTracking().Where(i => i.InvoiceDate >= start && i.InvoiceDate < end);
        var totals = await inv.GroupBy(_ => 1).Select(g => new
        {
            Count = g.Count(), Gross = g.Sum(i => i.GrossAmount), Discount = g.Sum(i => i.DiscountAmount),
            Net = g.Sum(i => i.NetAmount), Due = g.Sum(i => i.DueAmount), Patients = g.Select(i => i.PatientId).Distinct().Count(),
        }).FirstOrDefaultAsync();

        var pay = db.InvestigationPayments.AsNoTracking().Where(p => p.PaymentDate >= start && p.PaymentDate < end);
        var collection = await pay.Where(p => p.PaymentType != PaymentType.Refund).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var refund = await pay.Where(p => p.PaymentType == PaymentType.Refund).SumAsync(p => (decimal?)p.Amount) ?? 0;
        var totalDue = await db.Investigations.Where(i => i.DueAmount > 0).SumAsync(i => (decimal?)i.DueAmount) ?? 0;

        var status = await db.InvestigationDetails.AsNoTracking()
            .Where(d => d.Status != TestStatus.Delivered && d.Status != TestStatus.Cancelled)
            .GroupBy(d => new { d.Status, d.TestItem!.RequiresSample })
            .Select(g => new { g.Key.Status, g.Key.RequiresSample, Count = g.Count() }).ToListAsync();

        var weekStart = DateTime.Today.AddDays(-6);
        var daily = await db.Investigations.AsNoTracking().Where(i => i.InvoiceDate >= weekStart)
            .GroupBy(i => i.InvoiceDate.Date).Select(g => new { Date = g.Key, Count = g.Count(), Net = g.Sum(i => i.NetAmount) }).ToListAsync();
        var dailyPay = await db.InvestigationPayments.AsNoTracking().Where(p => p.PaymentDate >= weekStart)
            .GroupBy(p => p.PaymentDate.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(p => p.PaymentType == PaymentType.Refund ? -p.Amount : p.Amount) }).ToListAsync();

        var groups = await db.InvestigationDetails.AsNoTracking()
            .Where(d => d.Status != TestStatus.Cancelled && d.Investigation!.InvoiceDate >= start && d.Investigation.InvoiceDate < end)
            .GroupBy(d => d.TestItem!.TestGroup!.Name)
            .Select(g => new { Name = g.Key, Tests = g.Count(), Amount = g.Sum(d => d.NetAmount) })
            .OrderByDescending(g => g.Amount).Take(10).ToListAsync();

        int Count(Func<TestStatus, bool> s, bool? requiresSample = null) =>
            status.Where(x => s(x.Status) && (requiresSample == null || x.RequiresSample == requiresSample)).Sum(x => x.Count);

        var last7 = Enumerable.Range(0, 7).Select(i => weekStart.AddDays(i)).Select(d => new DailyPoint(d,
            daily.FirstOrDefault(x => x.Date == d)?.Count ?? 0,
            daily.FirstOrDefault(x => x.Date == d)?.Net ?? 0,
            dailyPay.FirstOrDefault(x => x.Date == d)?.Amount ?? 0)).ToList();

        return new DashboardSummary(
            totals?.Count ?? 0, totals?.Gross ?? 0, totals?.Discount ?? 0, totals?.Net ?? 0, collection, refund,
            totals?.Due ?? 0, totalDue, totals?.Patients ?? 0,
            Count(s => s is TestStatus.Ordered or TestStatus.SampleRejected, true),
            Count(s => s is TestStatus.SampleCollected or TestStatus.SampleReceived) + Count(s => s == TestStatus.Ordered, false),
            Count(s => s == TestStatus.ResultEntered),
            Count(s => s == TestStatus.Verified),
            Count(s => s == TestStatus.Finalized),
            await db.CancelRequests.CountAsync(c => c.Status == CancelStatus.Pending),
            last7, groups.Select(g => new GroupPoint(g.Name, g.Tests, g.Amount)).ToList());
    }
}
