using LlamaShears.Data.Abstractions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LlamaShears.Data.Entities;

/// <summary>
/// Join row linking an <see cref="Agent"/> to one of the
/// <see cref="Session"/>s it owns. Composite primary key on
/// <c>(AgentId, SessionId)</c>; a unique index on <c>SessionId</c>
/// alone enforces that any given session belongs to at most one
/// agent. The "active" session for an agent is the row with the
/// most recent <see cref="Created"/> for that agent — the table
/// therefore acts as both ownership ledger and history. No
/// <c>Id</c> column; no <c>LastModified</c> (rows are append-only).
/// </summary>
public class AgentSession : ICreated, IModelConfigurable<AgentSession>
{
    /// <summary>
    /// Foreign key to the owning <see cref="Agent"/>.
    /// </summary>
    public Guid AgentId { get; set; }

    /// <summary>
    /// Foreign key to the linked <see cref="Session"/>. Unique across
    /// the table.
    /// </summary>
    public Guid SessionId { get; set; }

    /// <inheritdoc />
    public DateTimeOffset Created { get; init; }

    /// <inheritdoc />
    public static void ConfigureModel(EntityTypeBuilder<AgentSession> entity)
    {
        entity.HasKey(nameof(AgentId), nameof(SessionId));
        entity.HasIndex(x => x.SessionId).IsUnique();
    }
}
