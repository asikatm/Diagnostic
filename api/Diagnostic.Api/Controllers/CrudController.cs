using System.Linq.Expressions;
using System.Reflection;
using Diagnostic.Api.Common;
using Diagnostic.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Controllers;

/// <summary>
/// Generic list / get / create / update / soft-delete for setup tables.
/// Any <c>?xxxId=5</c> query string is applied as an equality filter when the entity has that property.
/// </summary>
[ApiController]
public abstract class CrudController<T>(DiagnosticDbContext db) : ControllerBase where T : BaseEntity
{
    protected DiagnosticDbContext Db { get; } = db;
    private static readonly bool IsMaster = typeof(MasterEntity).IsAssignableFrom(typeof(T));

    /// <summary>Query used for list / get – override to Include() navigation data needed by the grid.</summary>
    protected virtual IQueryable<T> Query() => Db.Set<T>().AsNoTracking();

    protected virtual IQueryable<T> ApplySearch(IQueryable<T> q, string search) =>
        IsMaster
            ? q.Where(x => EF.Property<string>(x, "Name").Contains(search) || EF.Property<string>(x, "Code").Contains(search))
            : q;

    protected virtual IQueryable<T> ApplyOrder(IQueryable<T> q) =>
        IsMaster
            ? q.OrderBy(x => EF.Property<int>(x, "SortOrder")).ThenBy(x => EF.Property<string>(x, "Name"))
            : q.OrderByDescending(x => x.Id);

    /// <summary>Business validation before insert / update. Throw <see cref="AppException"/> to reject.</summary>
    protected virtual Task ValidateAsync(T model, T? existing) => Task.CompletedTask;

    /// <summary>Copy child collections on update (scalar columns are copied automatically).</summary>
    protected virtual Task OnUpdatingAsync(T existing, T model) => Task.CompletedTask;

    /// <summary>Load the tracked entity for update – override to Include() child collections.</summary>
    protected virtual Task<T?> FindForUpdateAsync(int id) => Db.Set<T>().FirstOrDefaultAsync(x => x.Id == id);

    [HttpGet]
    public virtual async Task<PagedResult<T>> List([FromQuery] PageQuery query)
    {
        var q = ApplyIdFilters(Query());
        if (!string.IsNullOrWhiteSpace(query.Search)) q = ApplySearch(q, query.Search.Trim());
        if (query.IsActive.HasValue) q = q.Where(x => x.IsActive == query.IsActive.Value);

        var pageSize = Math.Clamp(query.PageSize, 1, 1000);
        var page = Math.Max(query.Page, 1);
        var total = await q.CountAsync();
        var items = await ApplyOrder(q).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T> { Items = items, Total = total, Page = page, PageSize = pageSize };
    }

    /// <summary>Active rows as id/name pairs for dropdowns.</summary>
    [HttpGet("lookup")]
    public virtual async Task<List<LookupItem>> Lookup()
    {
        var q = ApplyIdFilters(Db.Set<T>().AsNoTracking().Where(x => x.IsActive));
        if (!IsMaster) return await q.OrderBy(x => x.Id).Select(x => new LookupItem(x.Id, x.Id.ToString(), null)).ToListAsync();
        return await ApplyOrder(q)
            .Select(x => new LookupItem(x.Id, EF.Property<string>(x, "Name"), EF.Property<string>(x, "Code")))
            .ToListAsync();
    }

    [HttpGet("{id:int}")]
    public virtual async Task<ActionResult<T>> Get(int id)
    {
        var item = await Query().FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : item;
    }

    [HttpPost]
    public virtual async Task<ActionResult<T>> Create(T model)
    {
        model.Id = 0;
        model.IsDeleted = false;
        await ValidateAsync(model, null);
        // Setting State on the entry (instead of Add) inserts only this row, never a navigation graph sent by the client,
        // except child collections the derived controller explicitly wants.
        Db.Entry(model).State = EntityState.Added;
        AttachChildrenForInsert(model);
        await Db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
    }

    [HttpPut("{id:int}")]
    public virtual async Task<ActionResult<T>> Update(int id, T model)
    {
        var existing = await FindForUpdateAsync(id);
        if (existing is null) return NotFound();

        model.Id = id;
        await ValidateAsync(model, existing);
        var entry = Db.Entry(existing);
        var createdAt = existing.CreatedAt;
        var createdBy = existing.CreatedBy;
        entry.CurrentValues.SetValues(model);
        existing.CreatedAt = createdAt;
        existing.CreatedBy = createdBy;
        existing.IsDeleted = false;
        await OnUpdatingAsync(existing, model);
        await Db.SaveChangesAsync();
        return existing;
    }

    [HttpDelete("{id:int}")]
    public virtual async Task<IActionResult> Delete(int id)
    {
        var existing = await Db.Set<T>().FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null) return NotFound();
        Db.Remove(existing); // converted to soft delete in DbContext
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Override to mark child collection rows as Added on create.</summary>
    protected virtual void AttachChildrenForInsert(T model) { }

    private IQueryable<T> ApplyIdFilters(IQueryable<T> q)
    {
        foreach (var (key, value) in Request.Query)
        {
            if (!key.EndsWith("Id", StringComparison.OrdinalIgnoreCase) || !int.TryParse(value, out var id)) continue;
            var prop = typeof(T).GetProperty(key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (prop is null || (prop.PropertyType != typeof(int) && prop.PropertyType != typeof(int?))) continue;

            var p = Expression.Parameter(typeof(T), "x");
            var body = Expression.Equal(Expression.Property(p, prop), Expression.Constant(id, prop.PropertyType));
            q = q.Where(Expression.Lambda<Func<T, bool>>(body, p));
        }
        return q;
    }
}
