using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent.Persistence;

/// <summary>
/// Live, mutable view of one agent's persisted conversation log.
/// Backed by an <see cref="IContextStore"/>; appending appends both
/// in-memory and to durable storage. Snapshots of <see cref="Turns"/>
/// and <see cref="Entries"/> are stable at the moment of access.
/// </summary>
public interface IAgentContext
{
    /// <summary>Identifier of the agent whose log this represents.</summary>
    string AgentId { get; }

    /// <summary>
    /// Snapshot of the conversation as <see cref="ModelTurn"/> values,
    /// filtered out of the polymorphic entry log. Stable for the duration
    /// of the call.
    /// </summary>
    IReadOnlyList<ModelTurn> Turns { get; }

    /// <summary>
    /// Snapshot of every persisted entry — turns and any future
    /// non-turn entry types — in arrival order.
    /// </summary>
    IReadOnlyList<IContextEntry> Entries { get; }

    /// <summary>
    /// Appends <paramref name="entry"/> to the live log and to the
    /// underlying store atomically. Subsequent reads of
    /// <see cref="Turns"/> / <see cref="Entries"/> include it.
    /// </summary>
    Task AppendAsync(IContextEntry entry, CancellationToken cancellationToken);

    /// <summary>
    /// Raised when the context is cleared in-memory (typically following
    /// <see cref="IContextStore.ClearAsync"/>). Subscribers should treat
    /// previously-observed entries as discarded.
    /// </summary>
    event EventHandler? Cleared;
}
