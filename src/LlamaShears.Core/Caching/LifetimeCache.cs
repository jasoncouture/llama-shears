using System.Collections.Concurrent;
using System.Diagnostics;

namespace LlamaShears.Core.Caching;

public sealed class LifetimeCache<TKey, TValue> : IAsyncDisposable
    where TKey : notnull
{
    private readonly Func<TKey, CancellationToken, Task<TValue>> _factory;
    private readonly TimeSpan _idleTimeout;
    private readonly ConcurrentDictionary<TKey, Entry> _entries;
    private int _disposed;

    public LifetimeCache(
        Func<TKey, CancellationToken, Task<TValue>> factory,
        TimeSpan idleTimeout,
        IEqualityComparer<TKey>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(factory);
        if (idleTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(idleTimeout), idleTimeout, "Idle timeout must be strictly positive.");
        }
        _factory = factory;
        _idleTimeout = idleTimeout;
        _entries = new ConcurrentDictionary<TKey, Entry>(comparer ?? EqualityComparer<TKey>.Default);
    }

    public async Task<TValue> GetOrCreateAsync(TKey key, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        await SweepIdleAsync().ConfigureAwait(false);
        var entry = _entries.GetOrAdd(key, k => new Entry(_factory.Invoke(k, CancellationToken.None)));
        entry.Touch();
        return await entry.Value.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<bool> InvalidateAsync(TKey key)
    {
        if (!_entries.TryRemove(key, out var entry))
        {
            return false;
        }
        await DisposeEntryAsync(entry).ConfigureAwait(false);
        return true;
    }

    private async Task SweepIdleAsync()
    {
        foreach (var kvp in _entries)
        {
            if (kvp.Value.IdleFor() < _idleTimeout) continue;
            if (!_entries.TryRemove(kvp.Key, out var removed)) continue;
            await DisposeEntryAsync(removed).ConfigureAwait(false);
        }
    }

    private static async ValueTask DisposeEntryAsync(Entry entry)
    {
        try
        {
            var value = await entry.Value.ConfigureAwait(false);
            switch (value)
            {
                case IAsyncDisposable a: await a.DisposeAsync().ConfigureAwait(false); break;
                case IDisposable d: d.Dispose(); break;
            }
        }
        catch
        {
            // Best-effort disposal; a faulted factory task or a disposal that
            // throws is not allowed to take down sweep/shutdown.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        foreach (var kvp in _entries)
        {
            await DisposeEntryAsync(kvp.Value).ConfigureAwait(false);
        }
        _entries.Clear();
    }

    private sealed class Entry
    {
        public Task<TValue> Value { get; }
        private long _lastTouch;

        public Entry(Task<TValue> value)
        {
            Value = value;
            _lastTouch = Stopwatch.GetTimestamp();
        }

        public void Touch() => Volatile.Write(ref _lastTouch, Stopwatch.GetTimestamp());

        public TimeSpan IdleFor() => Stopwatch.GetElapsedTime(Volatile.Read(ref _lastTouch));
    }
}
