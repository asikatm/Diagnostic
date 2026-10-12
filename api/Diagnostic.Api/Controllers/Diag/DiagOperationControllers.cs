using Diagnostic.Api.Common;
using Diagnostic.Api.Security;
using Diagnostic.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers.Diag;

/// <summary>Investigation Entry (billing), invoice list and due collection.</summary>
[ApiController, Route("api/diag/investigations"), MenuCode(MenuCodes.Investigation)]
public class InvestigationsController(InvestigationService service) : ControllerBase
{
    [HttpPost]
    public Task<CreateInvestigationResult> Create(CreateInvestigationDto dto) => service.CreateAsync(dto);

    [HttpGet, MenuCode(MenuCodes.InvoiceList)]
    public Task<PagedResult<InvoiceListItem>> List([FromQuery] InvoiceListQuery query) => service.ListAsync(query);

    [HttpGet("{id:int}"), MenuCode(MenuCodes.InvoiceList)]
    public async Task<ActionResult<InvoiceDto>> Get(int id) => await service.GetAsync(id) is { } dto ? dto : NotFound();

    [HttpGet("by-no/{invoiceNo}"), MenuCode(MenuCodes.InvoiceList)]
    public async Task<ActionResult<InvoiceDto>> GetByNo(string invoiceNo) => await service.GetAsync(null, invoiceNo) is { } dto ? dto : NotFound();

    [HttpPost("{id:int}/due-collection"), MenuCode(MenuCodes.InvoiceList), PermissionAction(PermissionAction.Add)]
    public async Task<IActionResult> CollectDue(int id, PaymentInput input)
    {
        var p = await service.CollectDueAsync(id, input);
        return Ok(new { paymentId = p.Id, p.ReceiptNo, p.Amount });
    }
}

[ApiController, Route("api/diag/cancel-requests")]
public class CancelRequestsController(CancelService service) : ControllerBase
{
    /// <summary>Cancel Service: request cancellation of an invoice or some of its tests.</summary>
    [HttpPost, MenuCode(MenuCodes.CancelService)]
    public async Task<IActionResult> Create(CancelRequestInput input) => Ok(new { ids = await service.RequestAsync(input) });

    [HttpGet, MenuCode(MenuCodes.CancelService)]
    public Task<List<CancelRequestDto>> List([FromQuery] CancelStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
        service.ListAsync(status, from, to);

    /// <summary>Approve Cancel: approve or reject a pending request.</summary>
    [HttpPost("{id:int}/decision"), MenuCode(MenuCodes.ApproveCancel), PermissionAction(PermissionAction.Edit)]
    public async Task<IActionResult> Decide(int id, CancelDecisionInput input)
    {
        await service.DecideAsync(id, input);
        return NoContent();
    }

    [HttpGet("pending"), MenuCode(MenuCodes.ApproveCancel)]
    public Task<List<CancelRequestDto>> Pending() => service.ListAsync(CancelStatus.Pending, null, null);
}

[ApiController, Route("api/diag/samples"), MenuCode(MenuCodes.SampleCollection)]
public class SamplesController(SampleService service) : ControllerBase
{
    [HttpGet("pending")]
    public Task<List<PendingSampleInvoice>> Pending([FromQuery] string? invoiceNo, [FromQuery] DateTime? date) => service.PendingAsync(invoiceNo, date);

    [HttpGet]
    public Task<List<SampleDto>> List([FromQuery] DateTime? date, [FromQuery] SampleStatus? status, [FromQuery] string? search) =>
        service.ListAsync(date, status, search);

    /// <summary>Collect one test, some tests, a whole invoice, or tests of many invoices at once.</summary>
    [HttpPost("collect")]
    public Task<List<SampleDto>> Collect(CollectSampleInput input) => service.CollectAsync(input);

    [HttpPost("dispatch"), PermissionAction(PermissionAction.Edit)]
    public Task<List<SampleDto>> Dispatch(SampleIdsInput input) => service.DispatchAsync(input.SampleIds);

    [HttpGet("repeat")]
    public Task<List<SampleDto>> RepeatPending([FromQuery] string? search) => service.RepeatPendingAsync(search);

    [HttpPost("repeat")]
    public Task<List<SampleDto>> Recollect(SampleIdsInput input) => service.RecollectAsync(input.SampleIds);
}

/// <summary>Sample Acknowledgement: the lab receives (acknowledges) or rejects dispatched tubes.</summary>
[ApiController, Route("api/diag/sample-ack"), MenuCode(MenuCodes.SampleAck)]
public class SampleAckController(SampleService service) : ControllerBase
{
    [HttpGet]
    public Task<List<SampleDto>> List([FromQuery] DateTime? date, [FromQuery] SampleStatus? status, [FromQuery] string? search) =>
        service.ListAsync(date, status, search);

    [HttpPost("scan/{sampleNo}"), PermissionAction(PermissionAction.Edit)]
    public Task<SampleDto> Scan(string sampleNo) => service.AcknowledgeByNoAsync(sampleNo);

    [HttpPost("acknowledge"), PermissionAction(PermissionAction.Edit)]
    public Task<List<SampleDto>> Acknowledge(SampleIdsInput input) => service.AcknowledgeAsync(input.SampleIds);

    [HttpPost("reject"), PermissionAction(PermissionAction.Edit)]
    public async Task<IActionResult> Reject(RejectSamplesInput input)
    {
        await service.RejectAsync(input.SampleIds, input.Reason);
        return NoContent();
    }
}

[ApiController, Route("api/diag/results"), MenuCode(MenuCodes.ResultEntry)]
public class ResultsController(LabResultService service) : ControllerBase
{
    [HttpGet("worklist")]
    public Task<List<WorklistItem>> Worklist([FromQuery] WorklistQuery query) => service.WorklistAsync(query);

    [HttpGet("{detailId:int}")]
    public Task<ResultEntryDto> Get(int detailId) => service.GetEntryAsync(detailId);

    [HttpPut("{detailId:int}")]
    public async Task<IActionResult> Save(int detailId, SaveResultInput input)
    {
        await service.SaveAsync(detailId, input);
        return NoContent();
    }

    [HttpPost("{detailId:int}/verify"), PermissionAction(PermissionAction.Edit)]
    public async Task<IActionResult> Verify(int detailId)
    {
        await service.VerifyAsync(detailId, true);
        return NoContent();
    }

    [HttpPost("{detailId:int}/unverify"), PermissionAction(PermissionAction.Edit)]
    public async Task<IActionResult> Unverify(int detailId)
    {
        await service.VerifyAsync(detailId, false);
        return NoContent();
    }

}

/// <summary>Lab Report Finalization – pathologist sign-off. Every action also checks the Pathologist role (not bypassed by Super Admin).</summary>
[ApiController, Route("api/diag/report-finalization"), MenuCode(MenuCodes.ReportFinalize)]
public class ReportFinalizationController(LabResultService service) : ControllerBase
{
    [HttpGet("can-finalize")]
    public async Task<IActionResult> CanFinalize()
    {
        var (allowed, message) = await service.CanFinalizeAsync();
        return Ok(new { allowed, message });
    }

    /// <summary>status=Verified (waiting) or Finalized (done).</summary>
    [HttpGet("worklist")]
    public Task<List<WorklistItem>> Worklist([FromQuery] WorklistQuery query) =>
        service.WorklistAsync(query with { Status = query.Status is TestStatus.Finalized ? TestStatus.Finalized : TestStatus.Verified });

    [HttpGet("{detailId:int}")]
    public Task<ResultEntryDto> Get(int detailId) => service.GetEntryAsync(detailId);

    [HttpPost("finalize"), PermissionAction(PermissionAction.Edit)]
    public async Task<IActionResult> Finalize(FinalizeInput input) => Ok(new { finalized = await service.FinalizeAsync(input) });

    [HttpPost("{detailId:int}/return"), PermissionAction(PermissionAction.Edit)]
    public async Task<IActionResult> Return(int detailId, ReturnInput input)
    {
        await service.ReturnToLabAsync(detailId, input.Reason);
        return NoContent();
    }

    [HttpPost("{detailId:int}/unfinalize"), PermissionAction(PermissionAction.Edit)]
    public async Task<IActionResult> Unfinalize(int detailId)
    {
        await service.UnfinalizeAsync(detailId);
        return NoContent();
    }
}

/// <summary>Report Delivery – hand over finalized reports to the patient.</summary>
[ApiController, Route("api/diag/report-delivery"), MenuCode(MenuCodes.ReportDelivery)]
public class ReportDeliveryController(LabResultService results, InvestigationService investigations) : ControllerBase
{
    /// <summary>Finalized reports waiting for delivery (status=Finalized) or delivered history (status=Delivered + dates).</summary>
    [HttpGet("worklist")]
    public Task<List<WorklistItem>> Worklist([FromQuery] WorklistQuery query) =>
        results.WorklistAsync(query with { Status = query.Status is TestStatus.Delivered ? TestStatus.Delivered : TestStatus.Finalized });

    [HttpGet("invoice/{invoiceNo}")]
    public async Task<ActionResult<InvoiceDto>> Invoice(string invoiceNo) =>
        await investigations.GetAsync(null, invoiceNo) is { } dto ? dto : NotFound(new { message = "Invoice not found." });

    [HttpPost("deliver"), PermissionAction(PermissionAction.Add)]
    public async Task<IActionResult> Deliver(DeliverInput input) => Ok(new { delivered = await results.DeliverAsync(input) });
}

[ApiController, Route("api/diag/dashboard"), MenuCode(MenuCodes.DiagDashboard)]
public class DashboardController(DashboardService service) : ControllerBase
{
    [HttpGet]
    public Task<DashboardSummary> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to) => service.GetAsync(from, to);
}

/// <summary>Dropdown data used by several diagnostic screens – only needs a logged-in user.</summary>
[ApiController, Route("api/diag/lookups")]
public class LookupsController(Data.DiagnosticDbContext db) : ControllerBase
{
    [HttpGet("billing")]
    public async Task<IActionResult> Billing()
    {
        var q = new
        {
            medias = await db.Medias.Where(d => d.IsActive).OrderBy(d => d.Name)
                .Select(d => new { d.Id, Name = d.Phone == null ? d.Name : d.Name + " (" + d.Phone + ")", d.Code }).ToListAsync(),
            doctors = await db.Doctors.Where(d => d.IsActive && d.IsReferrer).OrderBy(d => d.Name).Select(d => new LookupItem(d.Id, d.Name, d.Code)).ToListAsync(),
            signDoctors = await db.Doctors.Where(d => d.IsActive && d.IsReportSigner).OrderBy(d => d.Name).Select(d => new LookupItem(d.Id, d.Name, d.Code)).ToListAsync(),
            paymentModes = await db.PaymentModes.Where(d => d.IsActive).OrderBy(d => d.SortOrder).Select(d => new { d.Id, d.Name, d.Code, d.RequiresReference }).ToListAsync(),
            paymentChannels = await db.PaymentChannels.Where(d => d.IsActive).OrderBy(d => d.SortOrder).Select(d => new { d.Id, d.Name, d.PaymentModeId }).ToListAsync(),
            testGroups = await db.TestGroups.Where(d => d.IsActive).OrderBy(d => d.SortOrder).Select(d => new LookupItem(d.Id, d.Name, d.Code)).ToListAsync(),
            letterHeads = await db.LetterHeads.Where(d => d.IsActive).OrderByDescending(d => d.IsDefault).Select(d => new LookupItem(d.Id, d.Name, d.Code)).ToListAsync(),
        };
        return Ok(q);
    }
}
