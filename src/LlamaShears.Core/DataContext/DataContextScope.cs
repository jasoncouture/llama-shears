using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.DataContext;
internal sealed class DataContextScope : IDataContextScope
{
    private readonly Stack<ConcurrentDictionary<string, object?>> _stack =
        new Stack<ConcurrentDictionary<string, object?>>();
    private ConcurrentDictionary<string, object?> _current;

    public DataContextScope(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
        _current = new ConcurrentDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    public string Key { get; }

    public IDisposable BeginScope()
    {
        var copy = new ConcurrentDictionary<string, object?>(_current, StringComparer.OrdinalIgnoreCase);
        _stack.Push(_current);
        _current = copy;
        return new ScopePopper(this);
    }

    public void Clear() => _current.Clear();

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return _current.TryRemove(key, out _);
    }

    public async Task SetItemsAsync(IDataContextItemProvider provider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var items = await provider.GetItemsForCurrentContext(cancellationToken).ConfigureAwait(false);
        SetItems(items);
    }

    public void SetItems(IEnumerable<KeyValuePair<string, object?>> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        foreach (var pair in items)
        {
            _current[pair.Key] = pair.Value;
        }
    }

    public bool TryGetValue<T>(string key, out T? value) where T : class
    {
        if (_current.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    public ImmutableDictionary<string, object?> Snapshot() => [.. _current];

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _current.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void PopScope()
    {
        if (_stack.Count == 0)
        {
            return;
        }
        var previous = _stack.Pop();
        foreach (var pair in _current)
        {
            if (pair.Value is IPersistentDataContextItem)
            {
                previous[pair.Key] = pair.Value;
            }
        }
        _current = previous;
    }

    private sealed class ScopePopper : IDisposable
    {
        private readonly DataContextScope _owner;
        private bool _disposed;

        public ScopePopper(DataContextScope owner)
        {
            _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _owner.PopScope();
        }
    }
}
