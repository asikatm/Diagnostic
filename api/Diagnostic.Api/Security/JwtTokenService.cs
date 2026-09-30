using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Diagnostic.Api.Entities.Admin;
using Microsoft.IdentityModel.Tokens;

namespace Diagnostic.Api.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "Diagnostic";
    public string Audience { get; set; } = "Diagnostic";
    public string Key { get; set; } = string.Empty;
    public int ExpireMinutes { get; set; } = 720;
}

public class JwtTokenService(Microsoft.Extensions.Options.IOptions<JwtOptions> options)
{
    private readonly JwtOptions _opt = options.Value;

    public (string Token, DateTime ExpiresAt) Create(User user, IEnumerable<Role> roles)
    {
        var roleList = roles.ToList();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(AppClaims.UserId, user.Id.ToString()),
            new("name", user.FullName),
        };
        if (user.TenantId.HasValue) claims.Add(new Claim(AppClaims.TenantId, user.TenantId.Value.ToString()));
        if (roleList.Any(r => r.IsSuperAdmin)) claims.Add(new Claim(AppClaims.SuperAdmin, "1"));
        foreach (var r in roleList)
        {
            claims.Add(new Claim(AppClaims.RoleId, r.Id.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, r.Name));
        }

        var expires = DateTime.UtcNow.AddMinutes(_opt.ExpireMinutes);
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_opt.Issuer, _opt.Audience, claims, expires: expires, signingCredentials: creds);
        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
