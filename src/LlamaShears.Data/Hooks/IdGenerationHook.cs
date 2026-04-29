using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LlamaShears.Data.Hooks;

/// <summary>
/// Maintains the <see cref="IDataObject.Id"/> invariant.
/// <list type="bullet">
///   <item>On add: if <see cref="Guid.Empty"/>, assign a new UUIDv7;
///   otherwise leave the caller-supplied value untouched.</item>
///   <item>On update: throw if the id is <see cref="Guid.Empty"/> or
///   if the id property has been marked modified since materialization.</item>
/// </list>
/// No-op for entities that do not implement <see cref="IDataObject"/>
/// and for the deleted state.
/// </summary>
public sealed class IdGenerationHook : ISaveChangesHook
{
    public void Apply(EntityEntry entry, SaveChangesHookContext context)
    {
        if (entry.Entity is not IDataObject dataObject)
        {
            return;
        }

        switch (entry.State)
        {
            case EntityState.Added:
                if (dataObject.Id == Guid.Empty)
                {
                    entry.Property(nameof(IDataObject.Id)).CurrentValue = Guid.CreateVersion7();
                }
                break;

            case EntityState.Modified:
                var idProperty = entry.Property(nameof(IDataObject.Id));
                if (idProperty.IsModified)
                {
                    throw new InvalidOperationException(
                        $"Id on entity '{entry.Metadata.Name}' was modified after creation. " +
                        "Id is immutable.");
                }
                if (dataObject.Id == Guid.Empty)
                {
                    throw new InvalidOperationException(
                        $"Entity '{entry.Metadata.Name}' has Guid.Empty Id on update.");
                }
                break;
        }
    }
}
