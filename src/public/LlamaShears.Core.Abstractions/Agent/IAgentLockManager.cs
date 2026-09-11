namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Hands out exclusive locks keyed by session canonical id
/// (<c>agentId:guid:name</c>). Each key is backed by a dedicated
/// single-permit semaphore, so concurrent acquires for the same
/// session serialize; acquires for different sessions proceed
/// independently (including two sessions of the same agent).
/// Implementations are expected to be singleton.
/// </summary>
public interface IAgentLockManager
{
    /// <summary>
    /// Acquires the lock for <paramref name="key"/> (a session canonical
    /// id), waiting until any prior holder releases or until
    /// <paramref name="cancellationToken"/> fires. Returns a scope
    /// whose disposal releases the lock.
    /// </summary>
    ValueTask<ILockScope> AcquireLockAsync(string key, CancellationToken cancellationToken);
}
