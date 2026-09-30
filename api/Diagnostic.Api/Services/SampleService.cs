using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Diag;
using Diagnostic.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Services;

public record PendingSampleTest(int DetailId, int TestItemId, string TestName, string GroupName, int? VacutainerId, string? VacutainerName,
    string? ColorHex, string? Specimen, TestStatus Status, string? SampleNo);

public record PendingSampleInvoice(int InvestigationId, string InvoiceNo, DateTime InvoiceDate, string PatientCode, string PatientName,
    Gender Gender, string? PatientAge, List<PendingSampleTest> Tests);

/// <summary>Collect by invoice (all or some of its tests) or by any list of test lines across invoices.</summary>
public record CollectSampleInput(int? InvestigationId, List<int>? DetailIds);

public record SampleIdsInput(List<int> SampleIds);

public record RejectSamplesInput(List<int> SampleIds, string Reason);

public record SampleDto(int Id, string SampleNo, int InvestigationId, string InvoiceNo, string PatientName, string? PatientAge, Gender Gender,
    string? Vacutainer, string? ColorHex, string? Specimen, SampleStatus Status, DateTime CollectedAt, DateTime? DispatchedAt,
    DateTime? ReceivedAt, DateTime? RejectedAt, string? RejectReason, string? RepeatOfSampleNo, string Tests);

/// <summary>
/// Sample life cycle: Collect → Dispatch (collection room sends tube to lab) → Acknowledge (lab receives) or Reject.
/// A rejected tube keeps its tests (status SampleRejected) until it is collected again from "Repeat Sample".
/// </summary>
public class SampleService(DiagnosticDbContext db, NumberSequenceService numbers, ICurrentUser currentUser)
{
    // ------------------------------------------------------------------ collection

    /// <summary>Invoices that still have tests waiting for a sample (or one invoice with all its sample tests).</summary>
    public async Task<List<PendingSampleInvoice>> PendingAsync(string? invoiceNo, DateTime? date)
    {
        var q = db.Investigations.AsNoTracking().Where(i => i.Status != InvestigationStatus.Cancelled);
        if (!string.IsNullOrWhiteSpace(invoiceNo)) q = q.Where(i => i.InvoiceNo == invoiceNo.Trim());
        else
        {
            var day = (date ?? DateTime.Today).Date;
            q = q.Where(i => i.InvoiceDate >= day && i.InvoiceDate < day.AddDays(1)
                             && i.Details.Any(d => d.Status == TestStatus.Ordered && d.TestItem!.RequiresSample));
        }

        var invoices = await q.OrderByDescending(i => i.Id).Take(200)
            .Select(i => new
            {
                i.Id, i.InvoiceNo, i.InvoiceDate, i.Patient!.PatientCode, PatientName = i.Patient.Name, i.Patient.Gender, i.PatientAge,
                Tests = i.Details.Where(d => d.TestItem!.RequiresSample && d.Status != TestStatus.Cancelled)
                    .Select(d => new { d.Id, d.TestItemId, d.TestItem!.Name, d.TestItem.TestGroupId, Group = d.TestItem.TestGroup!.Name,
                        Specimen = d.TestItem.Specimen!.Name, d.Status, d.SampleCollection!.SampleNo }).ToList(),
            }).ToListAsync();

        var map = await VacutainerMap.LoadAsync(db);
        return invoices.Select(i => new PendingSampleInvoice(i.Id, i.InvoiceNo, i.InvoiceDate, i.PatientCode, i.PatientName, i.Gender, i.PatientAge,
            i.Tests.Select(t =>
            {
                var v = map.Resolve(t.TestItemId, t.TestGroupId);
                return new PendingSampleTest(t.Id, t.TestItemId, t.Name, t.Group, v?.Id, v?.Name, v?.ColorHex, t.Specimen, t.Status, t.SampleNo);
            }).ToList())).ToList();
    }

    /// <summary>Collects samples: one tube (sample no / barcode) per invoice and vacutainer type.</summary>
    public async Task<List<SampleDto>> CollectAsync(CollectSampleInput input)
    {
        if (input.InvestigationId is null && input.DetailIds is not { Count: > 0 })
            throw new AppException("Select the test(s) to collect.");

        await using var tx = await db.Database.BeginTransactionAsync();
        var q = db.InvestigationDetails.Include(d => d.TestItem)
            .Where(d => d.Status == TestStatus.Ordered && d.TestItem!.RequiresSample
                        && d.Investigation!.Status != InvestigationStatus.Cancelled);
        if (input.InvestigationId.HasValue) q = q.Where(d => d.InvestigationId == input.InvestigationId);
        if (input.DetailIds is { Count: > 0 }) q = q.Where(d => input.DetailIds.Contains(d.Id));
        var details = await q.ToListAsync();
        if (details.Count == 0) throw new AppException("No pending test for sample collection (already collected or cancelled).");

        var map = await VacutainerMap.LoadAsync(db);
        var created = new List<SampleCollection>();
        var groups = details.GroupBy(d => (d.InvestigationId, Vacu: map.Resolve(d.TestItemId, d.TestItem!.TestGroupId)?.Id, Specimen: d.TestItem!.SpecimenId));
        foreach (var grp in groups.OrderBy(g => g.Key.InvestigationId))
        {
            var sample = NewSample(grp.Key.InvestigationId, grp.Key.Vacu, grp.Key.Specimen, await numbers.NextAsync(NumberSequenceService.Sample));
            foreach (var d in grp) { d.Status = TestStatus.SampleCollected; sample.Details.Add(d); }
            db.SampleCollections.Add(sample);
            created.Add(sample);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await ByIdsAsync(created.Select(s => s.Id));
    }

    /// <summary>Sample Dispatch: collected tubes are sent to the lab.</summary>
    public async Task<List<SampleDto>> DispatchAsync(List<int> sampleIds)
    {
        var samples = await LoadAsync(sampleIds);
        var wrong = samples.Where(s => s.Status != SampleStatus.Collected).ToList();
        if (wrong.Count > 0) throw new AppException($"Only collected samples can be dispatched: {string.Join(", ", wrong.Select(s => $"{s.SampleNo} ({s.Status})"))}.");
        foreach (var s in samples)
        {
            s.Status = SampleStatus.Dispatched;
            s.DispatchedAt = DateTime.Now;
            s.DispatchedBy = currentUser.UserId;
        }
        await db.SaveChangesAsync();
        return await ByIdsAsync(sampleIds);
    }

    /// <summary>Repeat Sample: rejected tubes whose tests are still waiting for a new sample.</summary>
    public Task<List<SampleDto>> RepeatPendingAsync(string? search)
    {
        var q = db.SampleCollections.AsNoTracking()
            .Where(s => s.Status == SampleStatus.Rejected && s.Details.Any(d => d.Status == TestStatus.SampleRejected));
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(s => s.SampleNo == search || s.Investigation!.InvoiceNo == search);
        return ListQuery(q.OrderBy(s => s.RejectedAt)).ToListAsync();
    }

    /// <summary>Collects a new tube for each rejected sample and moves its tests to the new tube.</summary>
    public async Task<List<SampleDto>> RecollectAsync(List<int> rejectedSampleIds)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var rejected = await LoadAsync(rejectedSampleIds);
        var created = new List<SampleCollection>();
        foreach (var old in rejected)
        {
            var tests = old.Details.Where(d => d.Status == TestStatus.SampleRejected).ToList();
            if (old.Status != SampleStatus.Rejected || tests.Count == 0)
                throw new AppException($"{old.SampleNo} has no test waiting for a repeat sample.");

            var sample = NewSample(old.InvestigationId, old.VacutainerId, old.SpecimenId, await numbers.NextAsync(NumberSequenceService.Sample));
            sample.RepeatOfSampleId = old.Id;
            foreach (var d in tests)
            {
                old.Details.Remove(d);
                d.Status = TestStatus.SampleCollected;
                sample.Details.Add(d);
            }
            db.SampleCollections.Add(sample);
            created.Add(sample);
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return await ByIdsAsync(created.Select(s => s.Id));
    }

    // ------------------------------------------------------------------ lab acknowledgement

    /// <summary>Sample Acknowledgement: the lab confirms it has the tube (by scanned sample no).</summary>
    public async Task<SampleDto> AcknowledgeByNoAsync(string sampleNo)
    {
        var id = await db.SampleCollections.Where(x => x.SampleNo == sampleNo.Trim()).Select(x => (int?)x.Id).FirstOrDefaultAsync()
                 ?? throw new AppException($"Sample '{sampleNo}' not found.", 404);
        return (await AcknowledgeAsync([id]))[0];
    }

    public async Task<List<SampleDto>> AcknowledgeAsync(List<int> sampleIds)
    {
        var samples = await LoadAsync(sampleIds);
        foreach (var s in samples)
        {
            // Only tubes dispatched from Sample Collection can be acknowledged by the lab.
            if (s.Status != SampleStatus.Dispatched)
                throw new AppException(s.Status switch
                {
                    SampleStatus.Collected => $"{s.SampleNo} is not dispatched yet – dispatch it from Sample Collection first.",
                    SampleStatus.Received => $"{s.SampleNo} is already acknowledged.",
                    _ => $"{s.SampleNo} was rejected – collect a repeat sample.",
                });
            s.Status = SampleStatus.Received;
            s.ReceivedAt = DateTime.Now;
            s.ReceivedBy = currentUser.UserId;
            foreach (var d in s.Details.Where(d => d.Status == TestStatus.SampleCollected)) d.Status = TestStatus.SampleReceived;
        }
        await db.SaveChangesAsync();
        return await ByIdsAsync(sampleIds);
    }

    /// <summary>Rejects tubes (haemolysed, insufficient …). Their tests go to "Repeat Sample".</summary>
    public async Task RejectAsync(List<int> sampleIds, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new AppException("Reject reason is required.");
        var samples = await LoadAsync(sampleIds);
        foreach (var s in samples)
        {
            if (s.Status == SampleStatus.Rejected) throw new AppException($"{s.SampleNo} is already rejected.");
            if (s.Status == SampleStatus.Collected) throw new AppException($"{s.SampleNo} is not dispatched yet – the lab can only reject dispatched tubes.");
            if (s.Details.Any(d => d.Status is TestStatus.ResultEntered or TestStatus.Verified or TestStatus.Delivered))
                throw new AppException($"Result already entered for {s.SampleNo}; it cannot be rejected.");
            s.Status = SampleStatus.Rejected;
            s.RejectReason = reason.Trim();
            s.RejectedAt = DateTime.Now;
            s.RejectedBy = currentUser.UserId;
            foreach (var d in s.Details.Where(d => d.Status != TestStatus.Cancelled)) d.Status = TestStatus.SampleRejected;
        }
        await db.SaveChangesAsync();
    }

    // ------------------------------------------------------------------ queries

    public Task<List<SampleDto>> ListAsync(DateTime? date, SampleStatus? status, string? search)
    {
        var q = db.SampleCollections.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(s => s.SampleNo == search || s.Investigation!.InvoiceNo == search);
        else if (date.HasValue)
        {
            var day = date.Value.Date;
            q = q.Where(s => s.CollectedAt >= day && s.CollectedAt < day.AddDays(1));
        }
        if (status.HasValue) q = q.Where(s => s.Status == status);
        return ListQuery(q.OrderByDescending(s => s.Id).Take(1000)).ToListAsync();
    }

    private SampleCollection NewSample(int investigationId, int? vacutainerId, int? specimenId, string sampleNo) => new()
    {
        SampleNo = sampleNo,
        InvestigationId = investigationId,
        VacutainerId = vacutainerId,
        SpecimenId = specimenId,
        CollectedAt = DateTime.Now,
        CollectedBy = currentUser.UserId,
    };

    private async Task<List<SampleCollection>> LoadAsync(List<int> ids)
    {
        if (ids is not { Count: > 0 }) throw new AppException("Select at least one sample.");
        var list = await db.SampleCollections.Include(x => x.Details).Where(x => ids.Contains(x.Id)).ToListAsync();
        if (list.Count != ids.Distinct().Count()) throw new AppException("Some selected samples were not found.", 404);
        return list;
    }

    private Task<List<SampleDto>> ByIdsAsync(IEnumerable<int> ids)
    {
        var list = ids.ToList();
        return ListQuery(db.SampleCollections.AsNoTracking().Where(s => list.Contains(s.Id)).OrderBy(s => s.Id)).ToListAsync();
    }

    private static IQueryable<SampleDto> ListQuery(IQueryable<SampleCollection> q) => q.Select(s => new SampleDto(
        s.Id, s.SampleNo, s.InvestigationId, s.Investigation!.InvoiceNo, s.Investigation.Patient!.Name, s.Investigation.PatientAge,
        s.Investigation.Patient.Gender, s.Vacutainer!.Name, s.Vacutainer.ColorHex, s.Specimen!.Name, s.Status, s.CollectedAt, s.DispatchedAt,
        s.ReceivedAt, s.RejectedAt, s.RejectReason, s.RepeatOfSample!.SampleNo,
        string.Join(", ", s.Details.Where(d => d.Status != TestStatus.Cancelled).Select(d => d.TestItem!.ShortName ?? d.TestItem.Name))));
}

/// <summary>Vacu &amp; machine mapping lookup: item-level mapping wins over group-level mapping.</summary>
public class VacutainerMap
{
    private readonly List<VacuMachineMapping> _rows;
    private VacutainerMap(List<VacuMachineMapping> rows) => _rows = rows;

    public static async Task<VacutainerMap> LoadAsync(DiagnosticDbContext db) =>
        new(await db.VacuMachineMappings.AsNoTracking().Include(m => m.Vacutainer).Include(m => m.Machine)
            .Where(m => m.IsActive).ToListAsync());

    public Vacutainer? Resolve(int testItemId, int testGroupId) => Mapping(testItemId, testGroupId)?.Vacutainer;

    public VacuMachineMapping? Mapping(int testItemId, int testGroupId) =>
        _rows.FirstOrDefault(m => m.TestItemId == testItemId)
        ?? _rows.FirstOrDefault(m => m.TestItemId == null && m.TestGroupId == testGroupId);
}
