namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// An autonomous component that ingests input turns, drives a model,
/// and produces output turns. Identified by <see cref="Id"/>; the
/// rest of its surface — heartbeat cadence, channels, conversation
/// state — is internal and reachable through the services that own
/// it (config provider, context store, message bus).
/// </summary>
public interface IAgent : IDisposable
{
    /// <summary>Stable identifier for this agent.</summary>
    string Id { get; }

    /// <summary>
    /// Timestamp of the most recent turn recorded for this agent —
    /// i.e. the moment of last activity. <see langword="null"/> when
    /// the agent's context has no turns yet. Callers compute idle
    /// duration from this against the current wall clock.
    /// </summary>
    DateTimeOffset? LastActivity { get; }

    /// <summary>
    /// Acquires the agent's processing gate, blocking the run loop
    /// from starting any new batch until <see cref="UnlockAsync"/>
    /// is called. Pairs 1:1 with <see cref="UnlockAsync"/>; a caller
    /// that locks must unlock. Backed by a single-permit semaphore.
    /// </summary>
    Task LockAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Releases a permit previously acquired by <see cref="LockAsync"/>.
    /// </summary>
    ValueTask UnlockAsync();
}
