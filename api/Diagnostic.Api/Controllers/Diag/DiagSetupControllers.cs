using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Diag;
using Diagnostic.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers.Diag;

// Setup tables of the "diag" schema.

[Route("api/diag/parent-groups"), MenuCode(MenuCodes.ParentGroup)]
public class ParentGroupsController(DiagnosticDbContext db) : CrudController<ParentGroup>(db);

[Route("api/diag/test-groups"), MenuCode(MenuCodes.TestGroup)]
public class TestGroupsController(DiagnosticDbContext db) : CrudController<TestGroup>(db)
{
    protected override IQueryable<TestGroup> Query() => base.Query().Include(x => x.ParentGroup).Include(x => x.Department);
}

[Route("api/diag/specimens"), MenuCode(MenuCodes.Specimen)]
public class SpecimensController(DiagnosticDbContext db) : CrudController<Specimen>(db);

[Route("api/diag/vacutainers"), MenuCode(MenuCodes.Vacutainer)]
public class VacutainersController(DiagnosticDbContext db) : CrudController<Vacutainer>(db);

[Route("api/diag/machines"), MenuCode(MenuCodes.Machine)]
public class MachinesController(DiagnosticDbContext db) : CrudController<Machine>(db);

[Route("api/diag/doctors"), MenuCode(MenuCodes.Doctor)]
public class DoctorsController(DiagnosticDbContext db) : CrudController<Doctor>(db)
{
    protected override IQueryable<Doctor> ApplySearch(IQueryable<Doctor> q, string search) =>
        q.Where(x => x.Name.Contains(search) || x.Code!.Contains(search) || x.Phone!.Contains(search));
}

[Route("api/diag/test-items"), MenuCode(MenuCodes.TestItem)]
public class TestItemsController(DiagnosticDbContext db) : CrudController<TestItem>(db)
{
    protected override IQueryable<TestItem> Query() =>
        base.Query().Include(x => x.TestGroup).ThenInclude(g => g!.ParentGroup).Include(x => x.Specimen);

    protected override IQueryable<TestItem> ApplySearch(IQueryable<TestItem> q, string search) =>
        q.Where(x => x.Name.Contains(search) || x.Code!.Contains(search) || x.ShortName!.Contains(search));

    protected override Task ValidateAsync(TestItem model, TestItem? existing)
    {
        if (model.Price < 0) throw new AppException("Price cannot be negative.");
        if (model.MaxDiscountPercent is < 0 or > 100) throw new AppException("Max discount must be between 0 and 100.");
        return Task.CompletedTask;
    }

    /// <summary>Billing search: active tests with price (used by Investigation Entry).</summary>
    [HttpGet("search"), MenuCode(MenuCodes.Investigation), PermissionAction(PermissionAction.View)]
    public async Task<IActionResult> BillingSearch([FromQuery] string? q, [FromQuery] int take = 30) =>
        Ok(await Db.TestItems.AsNoTracking()
            .Where(x => x.IsActive && (q == null || x.Name.Contains(q) || x.Code!.Contains(q) || x.ShortName!.Contains(q)))
            .OrderBy(x => x.Name).Take(take)
            .Select(x => new { x.Id, x.Code, x.Name, x.Price, x.VatPercent, x.IsDiscountAllowed, x.MaxDiscountPercent, x.TestGroupId, Group = x.TestGroup!.Name })
            .ToListAsync());
}

[Route("api/diag/report-parameters"), MenuCode(MenuCodes.ReportParameter)]
public class ReportParametersController(DiagnosticDbContext db) : CrudController<ReportParameter>(db)
{
    protected override IQueryable<ReportParameter> ApplySearch(IQueryable<ReportParameter> q, string search) =>
        q.Where(x => x.Name.Contains(search));

    protected override IQueryable<ReportParameter> ApplyOrder(IQueryable<ReportParameter> q) =>
        q.OrderBy(x => x.TestItemId).ThenBy(x => x.SortOrder);

    protected override Task ValidateAsync(ReportParameter model, ReportParameter? existing)
    {
        if (model.LowValue.HasValue && model.HighValue.HasValue && model.LowValue > model.HighValue)
            throw new AppException("Low value cannot be greater than high value.");
        return Task.CompletedTask;
    }
}

[Route("api/diag/vacu-machine-mappings"), MenuCode(MenuCodes.VacuMachine)]
public class VacuMachineMappingsController(DiagnosticDbContext db) : CrudController<VacuMachineMapping>(db)
{
    protected override IQueryable<VacuMachineMapping> Query() => base.Query()
        .Include(x => x.TestGroup).Include(x => x.TestItem).Include(x => x.Vacutainer).Include(x => x.Machine);

    protected override IQueryable<VacuMachineMapping> ApplySearch(IQueryable<VacuMachineMapping> q, string search) =>
        q.Where(x => x.TestGroup!.Name.Contains(search) || x.TestItem!.Name.Contains(search) || x.Vacutainer!.Name.Contains(search));
}

[Route("api/diag/report-delivery-times"), MenuCode(MenuCodes.DeliveryTime)]
public class ReportDeliveryTimesController(DiagnosticDbContext db) : CrudController<ReportDeliveryTime>(db)
{
    protected override IQueryable<ReportDeliveryTime> Query() => base.Query().Include(x => x.TestGroup).Include(x => x.TestItem);

    protected override Task ValidateAsync(ReportDeliveryTime model, ReportDeliveryTime? existing)
    {
        if (model.TestGroupId is null && model.TestItemId is null) throw new AppException("Select a test group or a test item.");
        if (model.DeliveryHours < 0) throw new AppException("Delivery hours cannot be negative.");
        return Task.CompletedTask;
    }
}

[Route("api/diag/letter-heads"), MenuCode(MenuCodes.LetterHead)]
public class LetterHeadsController(DiagnosticDbContext db) : CrudController<LetterHead>(db)
{
    protected override IQueryable<LetterHead> Query() => base.Query().Include(x => x.Tenant);

    protected override async Task ValidateAsync(LetterHead model, LetterHead? existing)
    {
        // Only one default letter head per tenant.
        if (model.IsDefault)
            await Db.LetterHeads.Where(x => x.TenantId == model.TenantId && x.Id != model.Id && x.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsDefault, false));
    }
}

[Route("api/diag/discount-setups"), MenuCode(MenuCodes.Discount)]
public class DiscountSetupsController(DiagnosticDbContext db) : CrudController<DiscountSetup>(db)
{
    protected override IQueryable<DiscountSetup> Query() => base.Query().Include(x => x.TestGroup).Include(x => x.Doctor);

    protected override Task ValidateAsync(DiscountSetup model, DiscountSetup? existing)
    {
        if (model.Value < 0) throw new AppException("Discount value cannot be negative.");
        if (model.DiscountType == DiscountType.Percent && model.Value > 100) throw new AppException("Percent discount cannot exceed 100.");
        return Task.CompletedTask;
    }

    /// <summary>Discounts valid today (used by Investigation Entry).</summary>
    [HttpGet("available"), MenuCode(MenuCodes.Investigation), PermissionAction(PermissionAction.View)]
    public async Task<IActionResult> Available()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(await Db.DiscountSetups.AsNoTracking()
            .Where(x => x.IsActive && (x.ValidFrom == null || x.ValidFrom <= today) && (x.ValidTo == null || x.ValidTo >= today))
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.DiscountType, x.Value, x.TestGroupId, x.DoctorId })
            .ToListAsync());
    }
}

[Route("api/diag/opd-packages"), MenuCode(MenuCodes.OpdPackage)]
public class OpdPackagesController(DiagnosticDbContext db) : CrudController<OpdPackage>(db)
{
    protected override IQueryable<OpdPackage> Query() =>
        base.Query().Include(x => x.Items).ThenInclude(i => i.TestItem);

    protected override Task<OpdPackage?> FindForUpdateAsync(int id) =>
        Db.OpdPackages.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);

    protected override Task ValidateAsync(OpdPackage model, OpdPackage? existing)
    {
        if (model.Items.Count == 0) throw new AppException("A package needs at least one test.");
        if (model.Items.GroupBy(i => i.TestItemId).Any(g => g.Count() > 1)) throw new AppException("A test is added twice.");
        if (Math.Abs(model.Items.Sum(i => i.ItemPrice) - model.PackagePrice) > 0.01m)
            throw new AppException($"Sum of item prices ({model.Items.Sum(i => i.ItemPrice):N2}) must equal the package price ({model.PackagePrice:N2}).");
        foreach (var i in model.Items) i.TestItem = null; // never insert a test item from the posted graph
        return Task.CompletedTask;
    }

    protected override void AttachChildrenForInsert(OpdPackage model)
    {
        foreach (var i in model.Items) { i.Id = 0; Db.Entry(i).State = EntityState.Added; }
    }

    protected override Task OnUpdatingAsync(OpdPackage existing, OpdPackage model)
    {
        ChildSync.Sync(Db, existing.Items, model.Items, (t, s) => { t.TestItemId = s.TestItemId; t.ItemPrice = s.ItemPrice; });
        return Task.CompletedTask;
    }

    /// <summary>Packages valid today with their tests (used by Investigation Entry).</summary>
    [HttpGet("available"), MenuCode(MenuCodes.Investigation), PermissionAction(PermissionAction.View)]
    public async Task<IActionResult> Available()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return Ok(await Db.OpdPackages.AsNoTracking()
            .Where(x => x.IsActive && (x.ValidFrom == null || x.ValidFrom <= today) && (x.ValidTo == null || x.ValidTo >= today))
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id, x.Code, x.Name, x.PackagePrice,
                Items = x.Items.Select(i => new { i.TestItemId, TestName = i.TestItem!.Name, i.ItemPrice, i.TestItem.VatPercent, i.TestItem.TestGroupId }),
            })
            .ToListAsync());
    }
}

[Route("api/diag/patients"), MenuCode(MenuCodes.Patient)]
public class PatientsController(DiagnosticDbContext db, Services.NumberSequenceService numbers) : CrudController<Patient>(db)
{
    protected override IQueryable<Patient> ApplySearch(IQueryable<Patient> q, string search) =>
        q.Where(x => x.Name.Contains(search) || x.PatientCode.Contains(search) || x.Phone!.Contains(search));

    protected override async Task ValidateAsync(Patient model, Patient? existing)
    {
        if (string.IsNullOrWhiteSpace(model.Name)) throw new AppException("Patient name is required.");
        if (existing is null) model.PatientCode = await numbers.NextAsync(Services.NumberSequenceService.Patient);
        else model.PatientCode = existing.PatientCode;
    }

    /// <summary>Quick patient search for billing (by phone, code or name).</summary>
    [HttpGet("search"), MenuCode(MenuCodes.Investigation), PermissionAction(PermissionAction.View)]
    public async Task<IActionResult> Search([FromQuery] string q) =>
        Ok(await Db.Patients.AsNoTracking()
            .Where(x => x.PatientCode == q || x.Phone!.Contains(q) || x.Name.Contains(q))
            .OrderByDescending(x => x.Id).Take(20)
            .Select(x => new { x.Id, x.PatientCode, x.Name, x.Gender, x.DateOfBirth, x.Phone, x.Address, x.BloodGroup })
            .ToListAsync());
}
