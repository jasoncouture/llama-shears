using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.Core.Sessions;

public sealed class SessionFactory : ISessionFactory, IAsyncDisposable
{
    private readonly IServiceProvider _services;
    private readonly ConcurrentDictionary<SessionId, ISessionQueue> _sessions =
        new ConcurrentDictionary<SessionId, ISessionQueue>();

    public SessionFactory(IServiceProvider services)
    {
        _services = services;
    }

    public ISessionQueue Get(SessionId sessionId)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        return _sessions.GetOrAdd(
            sessionId,
            static (_, services) => ActivatorUtilities.CreateInstance<SessionQueue>(services),
            _services);
    }

    public bool TryGet(SessionId sessionId, [NotNullWhen(true)] out ISessionQueue? session)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        return _sessions.TryGetValue(sessionId, out session);
    }

    public async ValueTask DeleteAsync(SessionId sessionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        if (!_sessions.TryRemove(sessionId, out var session))
        {
            return;
        }
        await DisposeSessionAsync(session).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var pair in _sessions.ToArray())
        {
            if (_sessions.TryRemove(pair))
            {
                await DisposeSessionAsync(pair.Value).ConfigureAwait(false);
            }
        }
    }

    private static async ValueTask DisposeSessionAsync(ISessionQueue session)
    {
        switch (session)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }
}
