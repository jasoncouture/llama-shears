namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Hands out per-agent exclusive locks. Each agent id is backed by a
/// dedicated single-permit semaphore, so concurrent acquires for the
/// same agent serialize; acquires for different agents proceed
/// independently. Implementations are expected to be singleton.
/// </summary>
public interface IAgentLockManager
{
    /// <summary>
    /// Acquires the lock for <paramref name="agentId"/>, waiting until
    /// any prior holder releases or until <paramref name="cancellationToken"/>
    /// fires. Returns a scope whose disposal releases the lock.
    /// </summary>
    ValueTask<ILockScope> AcquireLockAsync(string agentId, CancellationToken cancellationToken);
}
