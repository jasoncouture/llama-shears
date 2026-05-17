namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Disposable handle to an acquired agent lock. The scope owns the
/// underlying semaphore permit; disposing it releases the permit and
/// flips <see cref="Active"/> to <see langword="false"/>. If the
/// holder forgets to dispose, the finalizer still releases the permit
/// so the lock isn't wedged for the rest of the process lifetime.
/// </summary>
public interface ILockScope : IDisposable
{
    /// <summary>
    /// <see langword="true"/> while the scope still holds the
    /// underlying lock; flips to <see langword="false"/> on the first
    /// successful disposal. A finalizer-only release does NOT flip
    /// this flag (the scope is already unreachable at that point), so
    /// callers should never read <see cref="Active"/> after losing
    /// their reference.
    /// </summary>
    bool Active { get; }
}
