using System.Globalization;
using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Diag;
using Diagnostic.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Services;

public record WorklistQuery(DateTime? From, DateTime? To, TestStatus? Status, int? TestGroupId, string? Search);

public record WorklistItem(int DetailId, int InvestigationId, string InvoiceNo, DateTime InvoiceDate, string PatientName, Gender Gender,
    string? PatientAge, string TestName, string GroupName, TestResultType ResultType, TestStatus Status, string? SampleNo, DateTime? DeliveryDateTime,
    DateTime? VerifiedAt, DateTime? FinalizedAt, string? FinalizedBy, decimal DueAmount);

public record FinalizeInput(List<int> DetailIds, int? SignDoctorId);
public record ReturnInput(string Reason);

public record ResultParameterDto(int ParameterId, string Name, string? GroupHeading, string? Unit, string? NormalRange, decimal? LowValue,
    decimal? HighValue, ParameterInputType InputType, string[] Options, string? Method, string? Value, ResultFlag Flag, string? Remarks);

public record ResultEntryDto(int DetailId, int InvestigationId, string InvoiceNo, string PatientCode, string PatientName, Gender Gender,
    string? PatientAge, string? ReferDoctor, string TestName, string GroupName, TestResultType ResultType, TestStatus Status, string? SampleNo,
    string? ReportText, string? Comments, int? SignDoctorId, bool IsVerified, List<ResultParameterDto> Parameters,
    bool IsFinalized = false, string? ReturnReason = null, string? VerifiedBy = null, DateTime? VerifiedAt = null);

public class ResultValueInput
{
    public int ParameterId { get; set; }
    public string? Value { get; set; }
    public string? Remarks { get; set; }
}

public class SaveResultInput
{
    public List<ResultValueInput> Results { get; set; } = [];
    public string? ReportText { get; set; }
    public string? Comments { get; set; }
    public int? SignDoctorId { get; set; }
    /// <summary>Save and verify in one step.</summary>
    public bool Verify { get; set; }
}

public record DeliverInput(int InvestigationId, string? DeliveredTo, List<int>? DetailIds);

public class LabResultService(DiagnosticDbContext db, ICurrentUser currentUser)
{
    private static readonly TestStatus[] EntryAllowed = [TestStatus.SampleCollected, TestStatus.SampleReceived, TestStatus.ResultEntered];

    public async Task<List<WorklistItem>> WorklistAsync(WorklistQuery q)
    {
        var query = db.InvestigationDetails.AsNoTracking().Where(d => d.Status != TestStatus.Cancelled);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(d => d.Investigation!.InvoiceNo == s || d.SampleCollection!.SampleNo == s || d.Investigation.Patient!.Name.Contains(s));
        }
        else if (q.From.HasValue || q.To.HasValue || q.Status is null)
        {
            // A status without dates (pending queues) shows everything waiting, whatever the bill date.
            var from = (q.From ?? DateTime.Today).Date;
            var to = (q.To ?? DateTime.Today).Date.AddDays(1);
            query = query.Where(d => d.Investigation!.InvoiceDate >= from && d.Investigation.InvoiceDate < to);
        }
        if (q.Status.HasValue) query = query.Where(d => d.Status == q.Status);
        if (q.TestGroupId.HasValue) query = query.Where(d => d.TestItem!.TestGroupId == q.TestGroupId);

        return await query.OrderBy(d => d.InvestigationId).ThenBy(d => d.Id).Take(1000)
            .Select(d => new WorklistItem(d.Id, d.InvestigationId, d.Investigation!.InvoiceNo, d.Investigation.InvoiceDate,
                d.Investigation.Patient!.Name, d.Investigation.Patient.Gender, d.Investigation.PatientAge, d.TestItem!.Name,
                d.TestItem.TestGroup!.Name, d.TestItem.ResultType, d.Status, d.SampleCollection!.SampleNo, d.DeliveryDateTime,
                db.LabReports.Where(r => r.InvestigationDetailId == d.Id).Select(r => r.VerifiedAt).FirstOrDefault(),
                db.LabReports.Where(r => r.InvestigationDetailId == d.Id).Select(r => r.FinalizedAt).FirstOrDefault(),
                db.LabReports.Where(r => r.InvestigationDetailId == d.Id)
                    .Select(r => db.Users.Where(u => u.Id == r.FinalizedBy).Select(u => u.FullName).FirstOrDefault()).FirstOrDefault(),
                d.Investigation.DueAmount))
            .ToListAsync();
    }

    public async Task<ResultEntryDto> GetEntryAsync(int detailId)
    {
        var d = await db.InvestigationDetails.AsNoTracking()
                    .Include(x => x.Investigation).ThenInclude(i => i!.Patient)
                    .Include(x => x.Investigation).ThenInclude(i => i!.ReferDoctor)
                    .Include(x => x.TestItem).ThenInclude(t => t!.TestGroup)
                    .Include(x => x.TestItem).ThenInclude(t => t!.Parameters)
                    .Include(x => x.SampleCollection)
                    .FirstOrDefaultAsync(x => x.Id == detailId) ?? throw new AppException("Test not found.", 404);

        var report = await db.LabReports.AsNoTracking().Include(r => r.Results)
            .FirstOrDefaultAsync(r => r.InvestigationDetailId == detailId);
        var values = report?.Results.ToDictionary(r => r.ReportParameterId) ?? [];
        var gender = d.Investigation!.Patient!.Gender;

        var parameters = d.TestItem!.Parameters.Where(p => p.IsActive).OrderBy(p => p.SortOrder).Select(p =>
        {
            values.TryGetValue(p.Id, out var v);
            return new ResultParameterDto(p.Id, p.Name, p.GroupHeading, p.Unit, RangeFor(p, gender), p.LowValue, p.HighValue, p.InputType,
                (p.Options ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), p.Method,
                v?.ResultValue ?? p.DefaultValue, v?.Flag ?? ResultFlag.Normal, v?.Remarks);
        }).ToList();

        var verifiedBy = report?.VerifiedBy is { } vb ? await db.Users.Where(u => u.Id == vb).Select(u => u.FullName).FirstOrDefaultAsync() : null;

        return new ResultEntryDto(d.Id, d.InvestigationId, d.Investigation.InvoiceNo, d.Investigation.Patient.PatientCode,
            d.Investigation.Patient.Name, gender, d.Investigation.PatientAge, d.Investigation.ReferDoctor?.Name, d.TestItem.Name,
            d.TestItem.TestGroup!.Name, d.TestItem.ResultType, d.Status, d.SampleCollection?.SampleNo,
            report?.ReportText ?? d.TestItem.DefaultReportText, report?.Comments, report?.SignDoctorId, report?.IsVerified ?? false, parameters,
            report?.IsFinalized ?? false, report?.ReturnReason, verifiedBy, report?.VerifiedAt);
    }

    public async Task SaveAsync(int detailId, SaveResultInput input)
    {
        var d = await db.InvestigationDetails.Include(x => x.TestItem).ThenInclude(t => t!.Parameters)
                    .Include(x => x.Investigation).ThenInclude(i => i!.Patient)
                    .FirstOrDefaultAsync(x => x.Id == detailId) ?? throw new AppException("Test not found.", 404);

        var canEnter = EntryAllowed.Contains(d.Status) || (d.Status == TestStatus.Ordered && !d.TestItem!.RequiresSample);
        if (!canEnter) throw new AppException(d.Status switch
        {
            TestStatus.Ordered => "Sample is not collected yet.",
            TestStatus.SampleRejected => "Sample was rejected – waiting for a repeat sample.",
            TestStatus.Verified => "Report is verified. Un-verify it before editing.",
            TestStatus.Finalized or TestStatus.Delivered => "Report is finalized by the pathologist and cannot be edited.",
            TestStatus.Cancelled => "Test is cancelled.",
            _ => $"Result cannot be entered in status {d.Status}.",
        });

        var report = await db.LabReports.Include(r => r.Results).FirstOrDefaultAsync(r => r.InvestigationDetailId == detailId);
        if (report is null)
        {
            report = new LabReport { InvestigationDetailId = detailId };
            db.LabReports.Add(report);
        }

        var parameters = d.TestItem!.Parameters.ToDictionary(p => p.Id);
        foreach (var v in input.Results)
        {
            if (!parameters.TryGetValue(v.ParameterId, out var p)) throw new AppException($"Parameter #{v.ParameterId} does not belong to this test.");
            var row = report.Results.FirstOrDefault(r => r.ReportParameterId == p.Id);
            if (row is null) report.Results.Add(row = new LabResult { ReportParameterId = p.Id });
            row.ResultValue = v.Value?.Trim();
            row.Remarks = v.Remarks;
            row.Flag = FlagFor(p, row.ResultValue);
        }

        report.ReportText = input.ReportText;
        report.Comments = input.Comments;
        report.SignDoctorId = input.SignDoctorId;
        report.EnteredBy = currentUser.UserId;
        report.EnteredAt = DateTime.Now;
        report.ReturnReason = null; // corrected after being sent back by the pathologist
        d.Status = TestStatus.ResultEntered;

        if (input.Verify) MarkVerified(report, d);
        await db.SaveChangesAsync();
    }

    public async Task VerifyAsync(int detailId, bool verify)
    {
        var d = await db.InvestigationDetails.FirstOrDefaultAsync(x => x.Id == detailId) ?? throw new AppException("Test not found.", 404);
        var report = await db.LabReports.FirstOrDefaultAsync(r => r.InvestigationDetailId == detailId)
                     ?? throw new AppException("Result is not entered yet.");
        if (verify)
        {
            if (d.Status != TestStatus.ResultEntered) throw new AppException($"Only entered results can be verified (status {d.Status}).");
            MarkVerified(report, d);
        }
        else
        {
            if (d.Status != TestStatus.Verified)
                throw new AppException(d.Status is TestStatus.Finalized or TestStatus.Delivered
                    ? "Report is finalized by the pathologist and cannot be un-verified."
                    : $"Report is not verified (status {d.Status}).");
            report.IsVerified = false;
            report.VerifiedAt = null;
            report.VerifiedBy = null;
            d.Status = TestStatus.ResultEntered;
        }
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ Lab Report Finalization (pathologist only)

    /// <summary>
    /// Only users having one of the roles in setting <c>Diag.FinalizeRoleCodes</c> (default PATHOLOGIST) can finalize.
    /// This is checked on purpose even for Super Admin – finalization is a medical sign-off, not an admin task.
    /// </summary>
    public async Task<(bool Allowed, string Message)> CanFinalizeAsync()
    {
        var setting = await db.AppGlobalSettings.AsNoTracking()
            .Where(s => s.SettingKey == "Diag.FinalizeRoleCodes").Select(s => s.SettingValue).FirstOrDefaultAsync();
        var codes = (string.IsNullOrWhiteSpace(setting) ? "PATHOLOGIST" : setting)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var roleIds = currentUser.RoleIds;
        var ok = await db.Roles.AnyAsync(r => roleIds.Contains(r.Id) && r.IsActive && codes.Contains(r.Code!));
        return ok ? (true, "") : (false, "Only a user with the Pathologist role can finalize lab reports.");
    }

    private async Task EnsureCanFinalizeAsync()
    {
        var (ok, msg) = await CanFinalizeAsync();
        if (!ok) throw new AppException(msg, StatusCodes.Status403Forbidden);
    }

    public async Task<int> FinalizeAsync(FinalizeInput input)
    {
        await EnsureCanFinalizeAsync();
        if (input.DetailIds is not { Count: > 0 }) throw new AppException("Select the report(s) to finalize.");

        var details = await db.InvestigationDetails.Include(d => d.TestItem).Where(d => input.DetailIds.Contains(d.Id)).ToListAsync();
        var notReady = details.Where(d => d.Status != TestStatus.Verified).ToList();
        if (notReady.Count > 0)
            throw new AppException($"Only verified reports can be finalized: {string.Join(", ", notReady.Select(d => $"{d.TestItem!.Name} ({d.Status})"))}.");

        var reports = await db.LabReports.Where(r => input.DetailIds.Contains(r.InvestigationDetailId)).ToListAsync();
        foreach (var d in details)
        {
            var r = reports.First(x => x.InvestigationDetailId == d.Id);
            if (input.SignDoctorId.HasValue) r.SignDoctorId = input.SignDoctorId;
            if (r.SignDoctorId is null) throw new AppException($"Select the signing doctor for {d.TestItem!.Name}.");
            r.IsFinalized = true;
            r.FinalizedBy = currentUser.UserId;
            r.FinalizedAt = DateTime.Now;
            d.Status = TestStatus.Finalized;
        }
        await db.SaveChangesAsync();
        return details.Count;
    }

    /// <summary>Pathologist sends a verified result back to the lab for correction.</summary>
    public async Task ReturnToLabAsync(int detailId, string reason)
    {
        await EnsureCanFinalizeAsync();
        if (string.IsNullOrWhiteSpace(reason)) throw new AppException("Write why the report is sent back.");
        var d = await db.InvestigationDetails.FirstOrDefaultAsync(x => x.Id == detailId) ?? throw new AppException("Test not found.", 404);
        if (d.Status != TestStatus.Verified) throw new AppException($"Only verified reports can be sent back (status {d.Status}).");
        var r = await db.LabReports.FirstAsync(x => x.InvestigationDetailId == detailId);
        r.IsVerified = false;
        r.VerifiedAt = null;
        r.VerifiedBy = null;
        r.ReturnReason = reason.Trim();
        d.Status = TestStatus.ResultEntered;
        await db.SaveChangesAsync();
    }

    /// <summary>Undo finalization (only before delivery).</summary>
    public async Task UnfinalizeAsync(int detailId)
    {
        await EnsureCanFinalizeAsync();
        var d = await db.InvestigationDetails.FirstOrDefaultAsync(x => x.Id == detailId) ?? throw new AppException("Test not found.", 404);
        if (d.Status != TestStatus.Finalized)
            throw new AppException(d.Status == TestStatus.Delivered ? "Report is already delivered." : $"Report is not finalized (status {d.Status}).");
        var r = await db.LabReports.FirstAsync(x => x.InvestigationDetailId == detailId);
        r.IsFinalized = false;
        r.FinalizedAt = null;
        r.FinalizedBy = null;
        d.Status = TestStatus.Verified;
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ Report Delivery

    public async Task<int> DeliverAsync(DeliverInput input)
    {
        var q = db.InvestigationDetails.Where(d => d.InvestigationId == input.InvestigationId && d.Status == TestStatus.Finalized);
        if (input.DetailIds is { Count: > 0 }) q = q.Where(d => input.DetailIds.Contains(d.Id));
        var details = await q.ToListAsync();
        if (details.Count == 0) throw new AppException("No finalized report to deliver. Reports must be finalized by the pathologist first.");

        var ids = details.Select(d => d.Id).ToList();
        var reports = await db.LabReports.Where(r => ids.Contains(r.InvestigationDetailId)).ToListAsync();
        foreach (var d in details) d.Status = TestStatus.Delivered;
        foreach (var r in reports) { r.DeliveredAt = DateTime.Now; r.DeliveredTo = input.DeliveredTo; }
        await db.SaveChangesAsync();
        return details.Count;
    }

    private void MarkVerified(LabReport report, InvestigationDetail d)
    {
        report.IsVerified = true;
        report.VerifiedBy = currentUser.UserId;
        report.VerifiedAt = DateTime.Now;
        d.Status = TestStatus.Verified;
    }

    public static string? RangeFor(ReportParameter p, Gender g) => g switch
    {
        Gender.Male when !string.IsNullOrWhiteSpace(p.NormalRangeMale) => p.NormalRangeMale,
        Gender.Female when !string.IsNullOrWhiteSpace(p.NormalRangeFemale) => p.NormalRangeFemale,
        _ => p.NormalRange,
    };

    public static ResultFlag FlagFor(ReportParameter p, string? value)
    {
        if (p.InputType != ParameterInputType.Numeric ||
            !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var n)) return ResultFlag.Normal;
        if (p.LowValue.HasValue && n < p.LowValue) return ResultFlag.Low;
        if (p.HighValue.HasValue && n > p.HighValue) return ResultFlag.High;
        return ResultFlag.Normal;
    }
}
