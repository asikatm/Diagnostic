using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Admin;
using Diagnostic.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers.Admin;

// Simple setup tables of the "admin" schema. Each controller only declares its route, menu code and grid includes.

[Route("api/admin/tenants"), MenuCode(MenuCodes.Tenants)]
public class TenantsController(DiagnosticDbContext db) : CrudController<Tenant>(db);

[Route("api/admin/departments"), MenuCode(MenuCodes.Departments)]
public class DepartmentsController(DiagnosticDbContext db) : CrudController<Department>(db)
{
    protected override IQueryable<Department> Query() => base.Query().Include(x => x.Tenant);
}

[Route("api/admin/service-units"), MenuCode(MenuCodes.ServiceUnits)]
public class ServiceUnitsController(DiagnosticDbContext db) : CrudController<ServiceUnit>(db)
{
    protected override IQueryable<ServiceUnit> Query() => base.Query().Include(x => x.Department);
}

[Route("api/admin/roles"), MenuCode(MenuCodes.Roles)]
public class RolesController(DiagnosticDbContext db) : CrudController<Role>(db);

[Route("api/admin/projects"), MenuCode(MenuCodes.Projects)]
public class ProjectsController(DiagnosticDbContext db) : CrudController<Project>(db);

[Route("api/admin/menus"), MenuCode(MenuCodes.ProjectMenus)]
public class MenusController(DiagnosticDbContext db) : CrudController<Menu>(db)
{
    protected override IQueryable<Menu> Query() => base.Query().Include(x => x.Project);

    protected override IQueryable<Menu> ApplyOrder(IQueryable<Menu> q) =>
        q.OrderBy(x => x.ProjectId).ThenBy(x => x.SortOrder).ThenBy(x => x.Name);

    protected override Task ValidateAsync(Menu model, Menu? existing)
    {
        if (existing != null && model.ParentId == existing.Id) throw new AppException("A menu cannot be its own parent.");
        return Task.CompletedTask;
    }
}

[Route("api/admin/payment-modes"), MenuCode(MenuCodes.PaymentMode)]
public class PaymentModesController(DiagnosticDbContext db) : CrudController<PaymentMode>(db);

[Route("api/admin/payment-channels"), MenuCode(MenuCodes.PaymentChannel)]
public class PaymentChannelsController(DiagnosticDbContext db) : CrudController<PaymentChannel>(db)
{
    protected override IQueryable<PaymentChannel> Query() => base.Query().Include(x => x.PaymentMode);
}

[Route("api/admin/countries"), MenuCode(MenuCodes.Country)]
public class CountriesController(DiagnosticDbContext db) : CrudController<Country>(db);

[Route("api/admin/religions"), MenuCode(MenuCodes.Religion)]
public class ReligionsController(DiagnosticDbContext db) : CrudController<Religion>(db);

[Route("api/admin/printers"), MenuCode(MenuCodes.Printer)]
public class PrintersController(DiagnosticDbContext db) : CrudController<Printer>(db)
{
    protected override IQueryable<Printer> Query() => base.Query().Include(x => x.Tenant);
}

[Route("api/admin/global-settings"), MenuCode(MenuCodes.GlobalSettings)]
public class AppGlobalSettingsController(DiagnosticDbContext db) : CrudController<AppGlobalSetting>(db)
{
    protected override IQueryable<AppGlobalSetting> ApplySearch(IQueryable<AppGlobalSetting> q, string search) =>
        q.Where(x => x.SettingKey.Contains(search) || (x.SettingGroup != null && x.SettingGroup.Contains(search)));

    protected override IQueryable<AppGlobalSetting> ApplyOrder(IQueryable<AppGlobalSetting> q) =>
        q.OrderBy(x => x.SettingGroup).ThenBy(x => x.SettingKey);
}

/// <summary>Reports and SP Mapping – a report with its stored-procedure parameters.</summary>
[Route("api/admin/reports"), MenuCode(MenuCodes.ReportSpMapping)]
public class ReportsController(DiagnosticDbContext db) : CrudController<Report>(db)
{
    protected override IQueryable<Report> Query() =>
        base.Query().Include(x => x.Project).Include(x => x.Parameters.Where(p => !p.IsDeleted).OrderBy(p => p.SortOrder));

    protected override Task<Report?> FindForUpdateAsync(int id) =>
        Db.Reports.Include(x => x.Parameters).FirstOrDefaultAsync(x => x.Id == id);

    protected override Task ValidateAsync(Report model, Report? existing)
    {
        if (!System.Text.RegularExpressions.Regex.IsMatch(model.ProcedureName, @"^[A-Za-z_][\w]*\.[A-Za-z_][\w]*$"))
            throw new AppException("Procedure name must be schema-qualified, e.g. diag.usp_Rpt_DailyCollection");
        foreach (var p in model.Parameters)
            if (!System.Text.RegularExpressions.Regex.IsMatch(p.ParamName, @"^@?[A-Za-z_]\w*$"))
                throw new AppException($"Invalid parameter name '{p.ParamName}'.");
        return Task.CompletedTask;
    }

    protected override void AttachChildrenForInsert(Report model)
    {
        foreach (var p in model.Parameters) { p.Id = 0; Db.Entry(p).State = EntityState.Added; }
    }

    protected override Task OnUpdatingAsync(Report existing, Report model)
    {
        ChildSync.Sync(Db, existing.Parameters, model.Parameters, (t, s) =>
        {
            t.ParamName = s.ParamName; t.Label = s.Label; t.DataType = s.DataType;
            t.IsRequired = s.IsRequired; t.DefaultValue = s.DefaultValue; t.SortOrder = s.SortOrder;
        });
        return Task.CompletedTask;
    }
}
