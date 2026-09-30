using System.Text;
using System.Text.Json.Serialization;
using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Diagnostic.Api.Entities.Admin;
using Diagnostic.Api.Security;
using Diagnostic.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------- Database
builder.Services.AddDbContext<DiagnosticDbContext>(o => o.UseSqlServer(
    builder.Configuration.GetConnectionString("DiagnosticDb"),
    sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", DiagnosticDbContext.AdminSchema)
              .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

// ---------------- Security
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
        NameClaimType = System.Security.Claims.ClaimTypes.Name,
        RoleClaimType = System.Security.Claims.ClaimTypes.Role,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
});
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<PermissionCache>();
builder.Services.AddScoped<PermissionFilter>();

// ---------------- Business services
builder.Services.AddScoped<NumberSequenceService>();
builder.Services.AddScoped<InvestigationService>();
builder.Services.AddScoped<CancelService>();
builder.Services.AddScoped<SampleService>();
builder.Services.AddScoped<LabResultService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<DbSeeder>();

builder.Services.AddControllers(o => o.Filters.AddService<PermissionFilter>())
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddOpenApi(o => o.AddDocumentTransformer((doc, _, _) =>
{
    doc.Info.Title = "Diagnostic API";
    doc.Info.Description = "HIS ERP – Diagnostic & Administrative modules. Login at /api/auth/login and use the token as Bearer.";
    return Task.CompletedTask;
}));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<DbSeeder>().RunAsync();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference("/docs", o => o.WithTitle("Diagnostic API")).AllowAnonymous();
app.MapGet("/", () => Results.Redirect("/docs")).AllowAnonymous().ExcludeFromDescription();
app.MapControllers();

app.Run();
