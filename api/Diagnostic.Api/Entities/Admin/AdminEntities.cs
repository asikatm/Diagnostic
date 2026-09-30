using System.Text.Json.Serialization;
using Diagnostic.Api.Common;

namespace Diagnostic.Api.Entities.Admin;

// ---------------- Organisation ----------------

/// <summary>A branch / hospital / diagnostic centre. Every user and invoice belongs to a tenant.</summary>
public class Tenant : MasterEntity
{
    public string? ShortName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
}

public class Department : MasterEntity
{
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
}

public class ServiceUnit : MasterEntity
{
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
}

// ---------------- Security ----------------

public class Role : MasterEntity
{
    public string? Description { get; set; }
    /// <summary>Super admin bypasses menu / report permission checks.</summary>
    public bool IsSuperAdmin { get; set; }
}

public class User : BaseEntity
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Designation { get; set; }
    [JsonIgnore] public string PasswordHash { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<UserRole> UserRoles { get; set; } = [];
}

public class UserRole
{
    public int UserId { get; set; }
    [JsonIgnore] public User? User { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
}

/// <summary>A top-level module shown as a tile on the home screen (Diagnostic, Administrative ...).</summary>
public class Project : MasterEntity
{
    public string? Icon { get; set; }
    public string? Url { get; set; }
}

/// <summary>Sidebar menu item of a project. <see cref="Code"/> is used by the API for permission checks.</summary>
public class Menu : MasterEntity
{
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? ParentId { get; set; }
    [JsonIgnore] public Menu? Parent { get; set; }
    public string? Url { get; set; }
    public string? Icon { get; set; }
}

public class MenuPermission : BaseEntity
{
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public int MenuId { get; set; }
    public Menu? Menu { get; set; }
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
    public bool CanPrint { get; set; }
}

// ---------------- Reports ----------------

/// <summary>"Reports and SP Mapping": a report is rendered by the report server from a stored procedure.</summary>
public class Report : MasterEntity
{
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string ProcedureName { get; set; } = string.Empty;
    /// <summary>Template key used by the report server (e.g. LabReport, MoneyReceipt, Sticker, Tabular).</summary>
    public string Template { get; set; } = "Tabular";
    public string? Orientation { get; set; } = "Portrait";
    public string? Description { get; set; }
    public List<ReportSpParameter> Parameters { get; set; } = [];
}

public class ReportSpParameter : BaseEntity
{
    public int ReportId { get; set; }
    [JsonIgnore] public Report? Report { get; set; }
    public string ParamName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    /// <summary>int, decimal, date, string, bool</summary>
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public int SortOrder { get; set; }
}

public class ReportPermission : BaseEntity
{
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public int ReportId { get; set; }
    public Report? Report { get; set; }
    public bool CanView { get; set; }
    public bool CanExport { get; set; }
}

// ---------------- Setup ----------------

public class PaymentMode : MasterEntity
{
    public bool RequiresReference { get; set; }
}

public class PaymentChannel : MasterEntity
{
    public int PaymentModeId { get; set; }
    public PaymentMode? PaymentMode { get; set; }
    public string? AccountNo { get; set; }
}

public class Country : MasterEntity
{
    public string? Nationality { get; set; }
    public string? PhoneCode { get; set; }
}

public class Religion : MasterEntity { }

public class AppGlobalSetting : BaseEntity
{
    public string SettingKey { get; set; } = string.Empty;
    public string? SettingValue { get; set; }
    public string? SettingGroup { get; set; }
    public string? Description { get; set; }
}

public class Printer : MasterEntity
{
    public PrinterType PrinterType { get; set; } = PrinterType.A4;
    public string? PrinterPath { get; set; }
    public string? PaperSize { get; set; }
    public int? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>Running counters for invoice / receipt / sample numbers.</summary>
public class NumberSequence
{
    public int Id { get; set; }
    public string SequenceKey { get; set; } = string.Empty;
    public string Prefix { get; set; } = string.Empty;
    /// <summary>Period the counter belongs to (yyMM); counter restarts when the period changes.</summary>
    public string Period { get; set; } = string.Empty;
    public int LastNumber { get; set; }
    public int Padding { get; set; } = 5;
}
