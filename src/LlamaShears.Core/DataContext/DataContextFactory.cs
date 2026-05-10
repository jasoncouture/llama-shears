using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Common;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core.DataContext;

internal sealed class DataContextFactory : IDataContextFactory
{
    public DataContextFactory([FromKeyedServices(DataContextConstants.SingletonKey)] IEnumerable<IDataContextItemProvider> providers)
    {
        _providers = [..providers];
    }
    private readonly Dictionary<string, WeakReference<IDataContextScope>> _scopes =
        new Dictionary<string, WeakReference<IDataContextScope>>(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new object();

    private readonly AsyncLocal<IDataContextScope?> _current = new AsyncLocal<IDataContextScope?>();
    private readonly ImmutableArray<IDataContextItemProvider> _providers;

    public IDataContextScope? Current => _current.Value;

    public async Task<IDataContextScope> StartContextAsync(string key, IEnumerable<IDataContextItemProvider> providers, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(providers);
        SweepDeadEntries();

        var scope = new DataContextScope(key);
        lock (_lock)
        {
            if (_scopes.TryGetValue(key, out var existing) && existing.TryGetTarget(out _))
            {
                throw new InvalidOperationException($"A data context with key '{key}' is already active.");
            }
            _scopes[key] = new WeakReference<IDataContextScope>(scope);
            _current.Value = scope;
        }

        try
        {
            foreach (var provider in _providers.Concat(providers))
            {
                await scope.SetItemsAsync(provider, cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            lock (_lock)
            {
                _scopes.Remove(key);
                if (ReferenceEquals(_current.Value, scope))
                {
                    _current.Value = null;
                }
            }
            throw;
        }
        return scope;
    }

    public bool TryJoinContextScope(string key, [NotNullWhen(true)] out IDataContextScope? context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        SweepDeadEntries();
        context = null;
        lock (_lock)
        {
            if (Current is not null)
            {
                if (string.Equals(Current.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    context = Current;
                    return true;
                }
                throw new InvalidOperationException("A scope is already present on this call chain; cannot join a different scope.");
            }
            if (!_scopes.TryGetValue(key, out var reference) || !reference.TryGetTarget(out var target))
            {
                if (reference is not null)
                {
                    _scopes.Remove(key);
                }
                return false;
            }
            _current.Value = target;
            context = target;
            return true;
        }
    }

    public void DeleteContext(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        lock (_lock)
        {
            if (Current is not null && string.Equals(Current.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                _current.Value = null;
            }
            _scopes.Remove(key);
        }
    }

    public void ClearCurrent(bool owner = false)
    {
        SweepDeadEntries();
        lock (_lock)
        {
            var current = Current;
            if (current is null)
            {
                return;
            }
            _current.Value = null;
            if (owner)
            {
                _scopes.Remove(current.Key);
            }
        }
    }

    private void SweepDeadEntries()
    {
        lock (_lock)
        {
            var dead = new List<string>();
            foreach (var pair in _scopes)
            {
                if (!pair.Value.TryGetTarget(out _))
                {
                    dead.Add(pair.Key);
                }
            }
            foreach (var key in dead)
            {
                _scopes.Remove(key);
            }
        }
    }
}
