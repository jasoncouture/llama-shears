using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LlamaShears.Data.Hooks;

/// <summary>
/// Maintains the <see cref="ICreated.Created"/> invariant.
/// <list type="bullet">
///   <item>On add: unconditionally set <c>Created</c> to
///   <see cref="SaveChangesHookContext.UtcNow"/>.</item>
///   <item>On update: throw if <c>Created</c> has been marked
///   modified since materialization.</item>
/// </list>
/// No-op for entities that do not implement <see cref="ICreated"/>
/// and for the deleted state.
/// </summary>
public sealed class CreatedHook : ISaveChangesHook
{
    public void Apply(EntityEntry entry, SaveChangesHookContext context)
    {
        if (entry.Entity is not ICreated)
        {
            return;
        }

        switch (entry.State)
        {
            case EntityState.Added:
                entry.Property(nameof(ICreated.Created)).CurrentValue = context.UtcNow;
                break;

            case EntityState.Modified:
                if (entry.Property(nameof(ICreated.Created)).IsModified)
                {
                    throw new InvalidOperationException(
                        $"Created on entity '{entry.Metadata.Name}' was modified after creation. " +
                        "Created is immutable.");
                }
                break;
        }
    }
}
