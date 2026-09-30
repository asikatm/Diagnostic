using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Admin;
using Diagnostic.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers.Admin;

public class UserSaveDto
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    public int? TenantId { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Required on create; on update leave empty to keep the current password.</summary>
    public string? Password { get; set; }
    public List<int> RoleIds { get; set; } = [];
}

public record UserDto(int Id, string UserName, string FullName, string? Email, string? Phone, string? Designation,
    int? TenantId, string? TenantName, int? DepartmentId, string? DepartmentName, bool IsActive, DateTime? LastLoginAt,
    List<int> RoleIds, string Roles);

[ApiController, Route("api/admin/users"), MenuCode(MenuCodes.Users)]
public class UsersController(DiagnosticDbContext db, IPasswordHasher<User> hasher) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<UserDto>> List([FromQuery] PageQuery query)
    {
        var q = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(x => x.UserName.Contains(query.Search) || x.FullName.Contains(query.Search) || x.Phone!.Contains(query.Search));
        if (query.IsActive.HasValue) q = q.Where(x => x.IsActive == query.IsActive);

        var total = await q.CountAsync();
        var items = await Project(q.OrderBy(x => x.UserName).Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)).ToListAsync();
        return new PagedResult<UserDto> { Items = items, Total = total, Page = query.Page, PageSize = query.PageSize };
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id)
    {
        var u = await Project(db.Users.AsNoTracking().Where(x => x.Id == id)).FirstOrDefaultAsync();
        return u is null ? NotFound() : u;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create(UserSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            throw new AppException("Password must be at least 6 characters.");
        await EnsureUniqueAsync(dto.UserName, 0);

        var user = new User();
        Map(dto, user);
        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        user.UserRoles = dto.RoleIds.Distinct().Select(r => new UserRole { RoleId = r }).ToList();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return (await Get(user.Id)).Value!;
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, UserSaveDto dto)
    {
        var user = await db.Users.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        await EnsureUniqueAsync(dto.UserName, id);

        Map(dto, user);
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            if (dto.Password.Length < 6) throw new AppException("Password must be at least 6 characters.");
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
        }

        var wanted = dto.RoleIds.Distinct().ToHashSet();
        user.UserRoles.RemoveAll(r => !wanted.Contains(r.RoleId));
        foreach (var r in wanted.Where(r => user.UserRoles.All(x => x.RoleId != r)))
            user.UserRoles.Add(new UserRole { RoleId = r });

        await db.SaveChangesAsync();
        return (await Get(id)).Value!;
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null) return NotFound();
        if (user.UserName.Equals("admin", StringComparison.OrdinalIgnoreCase)) throw new AppException("The built-in admin user cannot be deleted.");
        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task EnsureUniqueAsync(string userName, int id)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new AppException("User name is required.");
        if (await db.Users.AnyAsync(x => x.UserName == userName && x.Id != id))
            throw new AppException($"User name '{userName}' already exists.");
    }

    private static void Map(UserSaveDto dto, User user)
    {
        user.UserName = dto.UserName.Trim();
        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email;
        user.Phone = dto.Phone;
        user.Designation = dto.Designation;
        user.TenantId = dto.TenantId;
        user.DepartmentId = dto.DepartmentId;
        user.IsActive = dto.IsActive;
    }

    private static IQueryable<UserDto> Project(IQueryable<User> q) => q.Select(x => new UserDto(
        x.Id, x.UserName, x.FullName, x.Email, x.Phone, x.Designation,
        x.TenantId, x.Tenant!.Name, x.DepartmentId, x.Department!.Name, x.IsActive, x.LastLoginAt,
        x.UserRoles.Select(r => r.RoleId).ToList(),
        string.Join(", ", x.UserRoles.Select(r => r.Role!.Name))));
}
