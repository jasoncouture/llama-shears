using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core.DataContext;

internal sealed class DataContextFactory : IDataContextFactory
{
    private readonly ImmutableArray<IDataContextItemProvider> _providers;

    public DataContextFactory(
        [FromKeyedServices(DataContextConstants.SingletonKey)] IEnumerable<IDataContextItemProvider> providers)
    {
        _providers = [.. providers];
    }

    private readonly Dictionary<SessionId, WeakReference<IDataContextScope>> _scopes = [];

    private readonly object _lock = new object();

    private readonly AsyncLocal<IDataContextScope?> _current = new AsyncLocal<IDataContextScope?>();

    public IDataContextScope? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public IDataContextScope CreateContext(SessionId sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        SweepDeadEntries();

        var scope = new DataContextScope(sessionId);
        lock (_lock)
        {
            if (_scopes.TryGetValue(sessionId, out var existing) && existing.TryGetTarget(out _))
            {
                throw new InvalidOperationException($"A data context with key '{sessionId}' is already active.");
            }

            _scopes[sessionId] = new WeakReference<IDataContextScope>(scope);
            return _current.Value = scope;
        }
    }

    public async ValueTask InitializeAsync(SessionId sessionId, IEnumerable<IDataContextItemProvider> scopeProviders, IEnumerable<KeyValuePair<string, object?>> values, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        IDataContextScope? scope;
        lock (_lock)
        {
            if (!_scopes.TryGetValue(sessionId, out var weakScopeReference) || !weakScopeReference.TryGetTarget(out scope))
            {
                throw new InvalidOperationException($"No such scope exists: {sessionId}");
            }
        }

        var hadAny = scope.Any();
        scope.SetItems(values);
        if (!hadAny)
        {
            foreach (var provider in _providers)
            {
                await scope.SetItemsAsync(provider, cancellationToken);
            }
        }

        foreach (var provider in scopeProviders)
        {
            await scope.SetItemsAsync(provider, cancellationToken);
        }
    }

    public bool TryJoinContextScope(SessionId sessionId, [NotNullWhen(true)] out IDataContextScope? context)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        SweepDeadEntries();
        context = null;
        lock (_lock)
        {
            if (Current is not null)
            {
                if (Equals(Current.Key, sessionId))
                {
                    context = Current;
                    return true;
                }

                throw new InvalidOperationException(
                    "A scope is already present on this call chain; cannot join a different scope.");
            }

            if (!_scopes.TryGetValue(sessionId, out var reference) || !reference.TryGetTarget(out var target))
            {
                if (reference is not null)
                {
                    _scopes.Remove(sessionId);
                }

                return false;
            }

            _current.Value = target;
            context = target;
            return true;
        }
    }

    public void DeleteContext(SessionId sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        lock (_lock)
        {
            if (Current is not null && Equals(Current.Key, sessionId))
            {
                _current.Value = null;
            }

            _scopes.Remove(sessionId);
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
            var dead = new List<SessionId>();
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
