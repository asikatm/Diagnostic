using Diagnostic.Api.Common;
using Diagnostic.Api.Data;

namespace Diagnostic.Api.Controllers;

/// <summary>Synchronises a tracked child collection with the rows posted by the client (add / update / soft delete).</summary>
public static class ChildSync
{
    public static void Sync<TChild>(DiagnosticDbContext db, List<TChild> existing, List<TChild> incoming, Action<TChild, TChild> copy)
        where TChild : BaseEntity
    {
        var incomingIds = incoming.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();

        foreach (var old in existing.Where(x => !x.IsDeleted && !incomingIds.Contains(x.Id)).ToList())
            db.Remove(old); // soft delete

        foreach (var row in incoming)
        {
            var target = row.Id > 0 ? existing.FirstOrDefault(x => x.Id == row.Id) : null;
            if (target is null)
            {
                row.Id = 0;
                existing.Add(row);
            }
            else
            {
                copy(target, row);
                target.IsActive = row.IsActive;
            }
        }
    }
}
