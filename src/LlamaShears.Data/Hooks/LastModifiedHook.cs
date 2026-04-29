using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LlamaShears.Data.Hooks;

/// <summary>
/// Maintains the <see cref="ILastModified.LastModified"/> invariant:
/// on add or update, unconditionally set <c>LastModified</c> to
/// <see cref="SaveChangesHookContext.UtcNow"/>. No-op for entities
/// that do not implement <see cref="ILastModified"/> and for the
/// deleted state.
/// </summary>
public sealed class LastModifiedHook : ISaveChangesHook
{
    public void Apply(EntityEntry entry, SaveChangesHookContext context)
    {
        if (entry.Entity is not ILastModified)
        {
            return;
        }

        if (entry.State is EntityState.Added or EntityState.Modified)
        {
            entry.Property(nameof(ILastModified.LastModified)).CurrentValue = context.UtcNow;
        }
    }
}
