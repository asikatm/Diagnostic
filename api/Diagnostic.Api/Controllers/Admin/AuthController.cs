using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Admin;
using Diagnostic.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers.Admin;

public record LoginRequest(string UserName, string Password);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record LoginResponse(string Token, DateTime ExpiresAt, int UserId, string UserName, string FullName, int? TenantId,
    string? TenantName, bool IsSuperAdmin, List<string> Roles);
public record ProjectDto(int Id, string? Code, string Name, string? Icon, string? Url, List<MenuDto> Menus);
public record MenuDto(int Id, string? Code, string Name, string? Url, string? Icon, int? ParentId,
    bool CanView, bool CanAdd, bool CanEdit, bool CanDelete, bool CanPrint);

[ApiController, Route("api/auth")]
public class AuthController(DiagnosticDbContext db, IPasswordHasher<User> hasher, JwtTokenService jwt,
    ICurrentUser currentUser, PermissionCache permissionCache) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req)
    {
        var user = await db.Users.Include(u => u.Tenant).Include(u => u.UserRoles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.UserName == req.UserName);

        if (user is null || !user.IsActive ||
            hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            throw new AppException("Invalid user name or password.", StatusCodes.Status401Unauthorized);

        var roles = user.UserRoles.Select(r => r.Role!).Where(r => r is { IsActive: true, IsDeleted: false }).ToList();
        var (token, expires) = jwt.Create(user, roles);
        user.LastLoginAt = DateTime.Now;
        await db.SaveChangesAsync();

        return new LoginResponse(token, expires, user.Id, user.UserName, user.FullName, user.TenantId, user.Tenant?.Name,
            roles.Any(r => r.IsSuperAdmin), roles.Select(r => r.Name).ToList());
    }

    /// <summary>Projects (home tiles) and sidebar menus the logged-in user may see, with their rights.</summary>
    [HttpGet("menus")]
    public async Task<List<ProjectDto>> Menus()
    {
        var projects = await db.Projects.AsNoTracking().Where(p => p.IsActive).OrderBy(p => p.SortOrder).ToListAsync();
        var menus = await db.Menus.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToListAsync();
        var rights = currentUser.IsSuperAdmin ? null : await permissionCache.GetAsync(currentUser.RoleIds);

        var result = new List<ProjectDto>();
        foreach (var p in projects)
        {
            var items = new List<MenuDto>();
            foreach (var m in menus.Where(m => m.ProjectId == p.Id))
            {
                MenuRights r = rights is null
                    ? new MenuRights(true, true, true, true, true)
                    : (m.Code != null && rights.TryGetValue(m.Code, out var x) ? x : new MenuRights(false, false, false, false, false));
                // Parent (folder) menus without a URL are shown when any child is visible.
                if (r.View || m.Url == null)
                    items.Add(new MenuDto(m.Id, m.Code, m.Name, m.Url, m.Icon, m.ParentId, r.View, r.Add, r.Edit, r.Delete, r.Print));
            }
            items.RemoveAll(i => i.Url == null && items.All(c => c.ParentId != i.Id));
            if (items.Count > 0) result.Add(new ProjectDto(p.Id, p.Code, p.Name, p.Icon, p.Url, items));
        }
        return result;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Tenant).FirstOrDefaultAsync(u => u.Id == currentUser.UserId);
        if (user is null) return Unauthorized();
        return Ok(new { user.Id, user.UserName, user.FullName, user.Email, user.TenantId, TenantName = user.Tenant?.Name, currentUser.IsSuperAdmin });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest req)
    {
        var user = await db.Users.FirstAsync(u => u.Id == currentUser.UserId);
        if (hasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword) == PasswordVerificationResult.Failed)
            throw new AppException("Current password is not correct.");
        if (req.NewPassword.Length < 6) throw new AppException("New password must be at least 6 characters.");
        user.PasswordHash = hasher.HashPassword(user, req.NewPassword);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
