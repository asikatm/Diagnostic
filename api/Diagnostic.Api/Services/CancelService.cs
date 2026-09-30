using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Diag;
using Diagnostic.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Services;

public class CancelRequestInput
{
    public int InvestigationId { get; set; }
    /// <summary>Leave null to cancel the whole invoice.</summary>
    public List<int>? DetailIds { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
}

public record CancelDecisionInput(bool Approve, string? Remarks, int? RefundPaymentModeId);

public record CancelRequestDto(int Id, int InvestigationId, string InvoiceNo, string PatientName, int? InvestigationDetailId,
    string? TestName, decimal TestAmount, string Reason, decimal RefundAmount, CancelStatus Status, DateTime RequestedAt,
    string? RequestedBy, DateTime? ApprovedAt, string? ApprovalRemarks);

/// <summary>Cancel Service (request) and Approve Cancel (decision).</summary>
public class CancelService(DiagnosticDbContext db, InvestigationService investigations, ICurrentUser currentUser)
{
    private static readonly TestStatus[] NotCancellable = [TestStatus.Verified, TestStatus.Finalized, TestStatus.Delivered, TestStatus.Cancelled];

    public async Task<List<int>> RequestAsync(CancelRequestInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Reason)) throw new AppException("Reason is required.");
        if (input.RefundAmount < 0) throw new AppException("Refund amount cannot be negative.");

        var inv = await db.Investigations.Include(i => i.Details).ThenInclude(d => d.TestItem)
                      .FirstOrDefaultAsync(i => i.Id == input.InvestigationId) ?? throw new AppException("Invoice not found.", 404);
        if (inv.Status == InvestigationStatus.Cancelled) throw new AppException("Invoice is already cancelled.");

        // Refund is only possible for money paid beyond the invoice value that remains after cancellation.
        var cancelledValue = input.DetailIds is { Count: > 0 }
            ? inv.Details.Where(d => input.DetailIds.Contains(d.Id) && d.Status != TestStatus.Cancelled).Sum(d => d.NetAmount)
            : inv.NetAmount;
        var maxRefund = Math.Max(0, inv.PaidAmount - inv.RefundAmount - (inv.NetAmount - cancelledValue));
        if (input.RefundAmount > maxRefund)
            throw new AppException($"Refund cannot be more than {maxRefund:N2} (amount paid beyond the remaining bill).");

        var pending = await db.CancelRequests.Where(c => c.InvestigationId == inv.Id && c.Status == CancelStatus.Pending)
            .Select(c => c.InvestigationDetailId).ToListAsync();
        if (pending.Contains(null)) throw new AppException("A full-invoice cancel request is already pending.");

        var requests = new List<CancelRequest>();
        if (input.DetailIds is null || input.DetailIds.Count == 0)
        {
            var locked = inv.Details.Where(d => d.Status is TestStatus.Verified or TestStatus.Finalized or TestStatus.Delivered).ToList();
            if (locked.Count > 0) throw new AppException($"Report already verified for: {string.Join(", ", locked.Select(d => d.TestItem!.Name))}.");
            requests.Add(new CancelRequest { InvestigationId = inv.Id, Reason = input.Reason, RefundAmount = input.RefundAmount });
        }
        else
        {
            var refundLeft = input.RefundAmount;
            foreach (var id in input.DetailIds.Distinct())
            {
                var d = inv.Details.FirstOrDefault(x => x.Id == id) ?? throw new AppException($"Test line #{id} is not in this invoice.");
                if (NotCancellable.Contains(d.Status)) throw new AppException($"'{d.TestItem!.Name}' cannot be cancelled (status {d.Status}).");
                if (pending.Contains(d.Id)) throw new AppException($"A cancel request for '{d.TestItem!.Name}' is already pending.");
                var refund = Math.Min(refundLeft, d.NetAmount);
                refundLeft -= refund;
                requests.Add(new CancelRequest { InvestigationId = inv.Id, InvestigationDetailId = d.Id, Reason = input.Reason, RefundAmount = refund });
            }
            if (refundLeft > 0) throw new AppException("Refund amount is more than the value of the selected tests.");
        }

        db.CancelRequests.AddRange(requests);
        await db.SaveChangesAsync();
        return requests.Select(r => r.Id).ToList();
    }

    public async Task<List<CancelRequestDto>> ListAsync(CancelStatus? status, DateTime? from, DateTime? to)
    {
        var q = db.CancelRequests.AsNoTracking();
        if (status.HasValue) q = q.Where(c => c.Status == status);
        if (from.HasValue) q = q.Where(c => c.CreatedAt >= from.Value.Date);
        if (to.HasValue) q = q.Where(c => c.CreatedAt < to.Value.Date.AddDays(1));
        return await q.OrderByDescending(c => c.Id).Take(500)
            .Select(c => new CancelRequestDto(c.Id, c.InvestigationId, c.Investigation!.InvoiceNo, c.Investigation.Patient!.Name,
                c.InvestigationDetailId, c.InvestigationDetail!.TestItem!.Name,
                c.InvestigationDetailId == null ? c.Investigation.NetAmount : c.InvestigationDetail!.NetAmount,
                c.Reason, c.RefundAmount, c.Status, c.CreatedAt,
                db.Users.Where(u => u.Id == c.CreatedBy).Select(u => u.FullName).FirstOrDefault(),
                c.ApprovedAt, c.ApprovalRemarks))
            .ToListAsync();
    }

    public async Task DecideAsync(int requestId, CancelDecisionInput input)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var req = await db.CancelRequests.FirstOrDefaultAsync(c => c.Id == requestId) ?? throw new AppException("Request not found.", 404);
        if (req.Status != CancelStatus.Pending) throw new AppException("This request is already processed.");

        req.Status = input.Approve ? CancelStatus.Approved : CancelStatus.Rejected;
        req.ApprovedBy = currentUser.UserId;
        req.ApprovedAt = DateTime.Now;
        req.ApprovalRemarks = input.Remarks;

        if (input.Approve)
        {
            var inv = await db.Investigations.Include(i => i.Details).Include(i => i.Payments)
                .FirstAsync(i => i.Id == req.InvestigationId);
            var targets = req.InvestigationDetailId.HasValue
                ? inv.Details.Where(d => d.Id == req.InvestigationDetailId).ToList()
                : inv.Details.ToList();
            if (targets.Any(d => d.Status is TestStatus.Verified or TestStatus.Finalized or TestStatus.Delivered))
                throw new AppException("Report has been verified after the request was made; it cannot be cancelled now.");
            foreach (var d in targets) d.Status = TestStatus.Cancelled;

            InvoiceTotals.Recalculate(inv);
            var refund = Math.Min(req.RefundAmount, Math.Max(0, -inv.DueAmount)); // negative due = overpaid
            if (refund > 0)
            {
                var modeId = input.RefundPaymentModeId
                             ?? inv.Payments.Where(p => p.PaymentType != PaymentType.Refund).Select(p => p.PaymentModeId).LastOrDefault();
                inv.Payments.Add(await investigations.NewPaymentAsync(new PaymentInput { Amount = refund, PaymentModeId = modeId }, PaymentType.Refund));
            }
            InvoiceTotals.Recalculate(inv);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
