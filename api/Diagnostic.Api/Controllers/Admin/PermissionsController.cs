using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Admin;
using Diagnostic.Api.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers.Admin;

public record MenuPermissionRow(int MenuId, string MenuName, string? MenuCode, int ProjectId, string ProjectName, int? ParentId,
    bool CanView, bool CanAdd, bool CanEdit, bool CanDelete, bool CanPrint);

public record ReportPermissionRow(int ReportId, string ReportName, string? ReportCode, string? ProjectName, bool CanView, bool CanExport);

/// <summary>Role-wise menu permission matrix.</summary>
[ApiController, Route("api/admin/menu-permissions"), MenuCode(MenuCodes.MenuPermission)]
public class MenuPermissionsController(DiagnosticDbContext db, PermissionCache cache) : ControllerBase
{
    [HttpGet("{roleId:int}")]
    public async Task<List<MenuPermissionRow>> Get(int roleId, [FromQuery] int? projectId)
    {
        var perms = await db.MenuPermissions.AsNoTracking().Where(p => p.RoleId == roleId).ToDictionaryAsync(p => p.MenuId);
        var menus = await db.Menus.AsNoTracking().Include(m => m.Project)
            .Where(m => projectId == null || m.ProjectId == projectId)
            .OrderBy(m => m.Project!.SortOrder).ThenBy(m => m.SortOrder).ToListAsync();

        return menus.Select(m =>
        {
            perms.TryGetValue(m.Id, out var p);
            return new MenuPermissionRow(m.Id, m.Name, m.Code, m.ProjectId, m.Project!.Name, m.ParentId,
                p?.CanView ?? false, p?.CanAdd ?? false, p?.CanEdit ?? false, p?.CanDelete ?? false, p?.CanPrint ?? false);
        }).ToList();
    }

    [HttpPut("{roleId:int}")]
    public async Task<IActionResult> Save(int roleId, List<MenuPermissionRow> rows)
    {
        var existing = await db.MenuPermissions.Where(p => p.RoleId == roleId).ToDictionaryAsync(p => p.MenuId);
        foreach (var r in rows)
        {
            if (!existing.TryGetValue(r.MenuId, out var p))
            {
                if (!(r.CanView || r.CanAdd || r.CanEdit || r.CanDelete || r.CanPrint)) continue;
                p = new MenuPermission { RoleId = roleId, MenuId = r.MenuId };
                db.MenuPermissions.Add(p);
            }
            p.CanView = r.CanView; p.CanAdd = r.CanAdd; p.CanEdit = r.CanEdit; p.CanDelete = r.CanDelete; p.CanPrint = r.CanPrint;
        }
        await db.SaveChangesAsync();
        cache.Reset();
        return NoContent();
    }
}

/// <summary>Role-wise report permission matrix.</summary>
[ApiController, Route("api/admin/report-permissions"), MenuCode(MenuCodes.ReportPermission)]
public class ReportPermissionsController(DiagnosticDbContext db) : ControllerBase
{
    [HttpGet("{roleId:int}")]
    public async Task<List<ReportPermissionRow>> Get(int roleId)
    {
        var perms = await db.ReportPermissions.AsNoTracking().Where(p => p.RoleId == roleId).ToDictionaryAsync(p => p.ReportId);
        var reports = await db.Reports.AsNoTracking().Include(r => r.Project).OrderBy(r => r.SortOrder).ThenBy(r => r.Name).ToListAsync();
        return reports.Select(r =>
        {
            perms.TryGetValue(r.Id, out var p);
            return new ReportPermissionRow(r.Id, r.Name, r.Code, r.Project?.Name, p?.CanView ?? false, p?.CanExport ?? false);
        }).ToList();
    }

    [HttpPut("{roleId:int}")]
    public async Task<IActionResult> Save(int roleId, List<ReportPermissionRow> rows)
    {
        var existing = await db.ReportPermissions.Where(p => p.RoleId == roleId).ToDictionaryAsync(p => p.ReportId);
        foreach (var r in rows)
        {
            if (!existing.TryGetValue(r.ReportId, out var p))
            {
                if (!r.CanView && !r.CanExport) continue;
                p = new ReportPermission { RoleId = roleId, ReportId = r.ReportId };
                db.ReportPermissions.Add(p);
            }
            p.CanView = r.CanView; p.CanExport = r.CanExport;
        }
        await db.SaveChangesAsync();
        return NoContent();
    }
}
