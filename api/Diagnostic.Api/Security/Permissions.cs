using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Diagnostic.Api.Security;

/// <summary>
/// Ties a controller (or action) to an <c>admin.Menus.Code</c>. Access is then decided by <c>admin.MenuPermissions</c>
/// of the user's roles. The action is inferred from the HTTP verb (GET=View, POST=Add, PUT=Edit, DELETE=Delete)
/// unless <see cref="PermissionActionAttribute"/> says otherwise.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MenuCodeAttribute(string code) : Attribute
{
    public string Code { get; } = code;
}

[AttributeUsage(AttributeTargets.Method)]
public class PermissionActionAttribute(PermissionAction action) : Attribute
{
    public PermissionAction Action { get; } = action;
}

public record MenuRights(bool View, bool Add, bool Edit, bool Delete, bool Print)
{
    public bool Allows(PermissionAction a) => a switch
    {
        PermissionAction.View => View,
        PermissionAction.Add => Add,
        PermissionAction.Edit => Edit,
        PermissionAction.Delete => Delete,
        PermissionAction.Print => Print,
        _ => false,
    };
}

/// <summary>Caches role → menu rights; call <see cref="Reset"/> after permissions are changed.</summary>
public class PermissionCache(IMemoryCache cache, IServiceScopeFactory scopeFactory)
{
    private CancellationTokenSource _cts = new();

    public async Task<Dictionary<string, MenuRights>> GetAsync(IReadOnlyList<int> roleIds)
    {
        var key = "perm:" + string.Join(',', roleIds.Order());
        if (cache.TryGetValue(key, out Dictionary<string, MenuRights>? rights) && rights != null) return rights;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DiagnosticDbContext>();
        var rows = await db.MenuPermissions.AsNoTracking()
            .Where(p => roleIds.Contains(p.RoleId) && p.IsActive && p.Menu!.Code != null)
            .Select(p => new { p.Menu!.Code, p.CanView, p.CanAdd, p.CanEdit, p.CanDelete, p.CanPrint })
            .ToListAsync();

        rights = rows.GroupBy(r => r.Code!, StringComparer.OrdinalIgnoreCase).ToDictionary(
            g => g.Key,
            g => new MenuRights(g.Any(x => x.CanView), g.Any(x => x.CanAdd), g.Any(x => x.CanEdit),
                                g.Any(x => x.CanDelete), g.Any(x => x.CanPrint)),
            StringComparer.OrdinalIgnoreCase);

        cache.Set(key, rights, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30))
            .AddExpirationToken(new CancellationChangeToken(_cts.Token)));
        return rights;
    }

    public void Reset()
    {
        var old = Interlocked.Exchange(ref _cts, new CancellationTokenSource());
        old.Cancel();
        old.Dispose();
    }
}

public class PermissionFilter(PermissionCache permissionCache, ICurrentUser currentUser) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var metadata = context.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any()) return;
        if (context.HttpContext.User.Identity?.IsAuthenticated != true) return; // authentication middleware answers 401

        // Method-level attribute wins over class-level (it comes later in the metadata list).
        var menu = metadata.OfType<MenuCodeAttribute>().LastOrDefault();
        if (menu is null || currentUser.IsSuperAdmin) return;

        var action = metadata.OfType<PermissionActionAttribute>().LastOrDefault()?.Action
                     ?? context.HttpContext.Request.Method switch
                     {
                         "POST" => PermissionAction.Add,
                         "PUT" or "PATCH" => PermissionAction.Edit,
                         "DELETE" => PermissionAction.Delete,
                         _ => PermissionAction.View,
                     };

        var rights = await permissionCache.GetAsync(currentUser.RoleIds);
        if (rights.TryGetValue(menu.Code, out var r) && r.Allows(action)) return;

        context.Result = new ObjectResult(new { message = $"You do not have '{action}' permission on '{menu.Code}'." })
        {
            StatusCode = StatusCodes.Status403Forbidden,
        };
    }
}
