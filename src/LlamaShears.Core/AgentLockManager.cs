using System.Collections.Concurrent;
using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core;

public sealed class AgentLockManager : IAgentLockManager
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores =
        new(StringComparer.Ordinal);

    public async ValueTask<ILockScope> AcquireLockAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        var semaphore = _semaphores.GetOrAdd(agentId, static _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken);
        return new LockScope(semaphore);
    }

    private sealed class LockScope : ILockScope
    {
        private readonly SemaphoreSlim _semaphore;
        private int _disposed;

        public LockScope(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
            Active = true;
        }

        public bool Active { get; private set; }

        ~LockScope()
        {
            // Last-resort release: if the holder forgot to Dispose, the
            // finalizer still returns the permit so the semaphore isn't
            // wedged for the rest of the process lifetime.
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }
            Active = false;
            _semaphore.Release();
            GC.SuppressFinalize(this);
        }
    }
}
