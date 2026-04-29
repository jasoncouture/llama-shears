using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LlamaShears.Data.Entities;

/// <summary>
/// Persistent identity for an agent. Agent configuration (model,
/// parameters, on-disk path, etc.) is intentionally not persisted in
/// the database; this row exists so that sessions can be linked to a
/// stable agent id and a stable, unique name. Configuration storage
/// will be revisited as a separate design decision.
/// </summary>
public class Agent : IDataObject, ICreated, ILastModified, IModelConfigurable<Agent>
{
    /// <inheritdoc />
    public Guid Id { get; init; }

    /// <inheritdoc />
    public DateTimeOffset Created { get; init; }

    /// <inheritdoc />
    public DateTimeOffset LastModified { get; set; }

    /// <summary>
    /// Unique, immutable name for the agent. The property itself
    /// carries no logic; format and immutability are enforced at
    /// save time by <c>LlamaShears.Data.Hooks.AgentNameHook</c>. The
    /// only accepted form is the canonical one: a single lowercase
    /// ASCII letter followed by zero or more lowercase ASCII letters
    /// or digits (regex <c>^[a-z][a-z0-9]*$</c>). Non-canonical input
    /// is rejected; callers are expected to normalize before
    /// persisting. Uniqueness is enforced by the DB via the index
    /// declared in <see cref="ConfigureModel"/>.
    /// </summary>
    public required string Name { get; init; } = string.Empty;

    /// <inheritdoc />
    public static void ConfigureModel(EntityTypeBuilder<Agent> entity)
    {
        entity.Property(a => a.Name).IsRequired();
        entity.HasIndex(a => a.Name).IsUnique();

        entity.HasMany<AgentSession>()
            .WithOne()
            .HasForeignKey(x => x.AgentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
