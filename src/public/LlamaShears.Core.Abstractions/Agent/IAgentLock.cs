namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Scoped passthrough over <see cref="IAgentLockManager"/> that
/// resolves the current session canonical id from the ambient data
/// scope, so callers don't have to thread the id through every
/// acquire site. Use this from inside an agent-owned scope; resolve
/// <see cref="IAgentLockManager"/> directly when you need to lock
/// an arbitrary session.
/// </summary>
public interface IAgentLock
{
    /// <summary>
    /// Acquires the lock for the session owning the current data
    /// scope, waiting until any prior holder releases or until
    /// <paramref name="cancellationToken"/> fires. Returns a scope
    /// whose disposal releases the lock.
    /// </summary>
    ValueTask<ILockScope> AcquireLockAsync(CancellationToken cancellationToken);
}
