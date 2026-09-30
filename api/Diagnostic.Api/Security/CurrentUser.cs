using System.Security.Claims;

namespace Diagnostic.Api.Security;

public interface ICurrentUser
{
    int? UserId { get; }
    int? TenantId { get; }
    string? UserName { get; }
    bool IsSuperAdmin { get; }
    IReadOnlyList<int> RoleIds { get; }
}

public static class AppClaims
{
    public const string UserId = "uid";
    public const string TenantId = "tid";
    public const string RoleId = "rid";
    public const string SuperAdmin = "sa";
}

public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int? UserId => int.TryParse(Principal?.FindFirstValue(AppClaims.UserId), out var id) ? id : null;
    public int? TenantId => int.TryParse(Principal?.FindFirstValue(AppClaims.TenantId), out var id) ? id : null;
    public string? UserName => Principal?.Identity?.Name;
    public bool IsSuperAdmin => Principal?.FindFirstValue(AppClaims.SuperAdmin) == "1";

    public IReadOnlyList<int> RoleIds =>
        Principal?.FindAll(AppClaims.RoleId).Select(c => int.Parse(c.Value)).ToList() ?? [];
}
