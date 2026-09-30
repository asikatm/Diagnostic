namespace Diagnostic.Api.Common;

/// <summary>
/// Every table carries the same audit / soft-delete columns so data can be traced and never physically lost.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Master tables that have a short unique code and a display name.</summary>
public abstract class MasterEntity : BaseEntity
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
