using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class FakeDataContextScope : IDataContextScope
{
    private readonly ConcurrentDictionary<string, object?> _items =
        new ConcurrentDictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

    public FakeDataContextScope(SessionId key)
    {
        Key = key;
    }

    public SessionId Key { get; }

    public bool TryGetValue<T>(string key, [NotNullWhen(true)] out T? value) where T : class
    {
        if (_items.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    public IDisposable BeginScope() => NoopDisposable.Instance;

    public void SetItems(IDataContextItemProvider provider, CancellationToken cancellationToken)
    {
    }

    public void SetItems(IEnumerable<KeyValuePair<string, object?>> items)
    {
        foreach (var pair in items)
        {
            _items[pair.Key] = pair.Value;
        }
    }

    public void Clear() => _items.Clear();

    public bool Remove(string key) => _items.TryRemove(key, out _);

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new NoopDisposable();
        public void Dispose() { }
    }
}
