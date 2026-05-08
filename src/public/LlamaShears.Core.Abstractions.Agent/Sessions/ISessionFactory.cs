using System.Diagnostics.CodeAnalysis;

namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Per-agent registry of live sessions. Backed by a concurrent
/// dictionary keyed by <see cref="SessionId"/>; sessions are created
/// on first <see cref="Get"/> via <c>ActivatorUtilities</c> and reused
/// on subsequent requests.
/// <para>
/// Today the per-session surface is just <see cref="ISessionQueue"/>;
/// the return type will broaden when the session interface lands.
/// </para>
/// </summary>
public interface ISessionFactory
{
    /// <summary>
    /// Returns the session for <paramref name="sessionId"/>, creating
    /// it if absent (matches <see cref="Dictionary{TKey, TValue}"/>'s indexer).
    /// </summary>
    ISessionQueue Get(SessionId sessionId);

    /// <summary>
    /// Returns the existing session for <paramref name="sessionId"/>
    /// without creating a new one. <see langword="true"/> when present
    /// (matches <see cref="Dictionary{TKey, TValue}.TryGetValue"/>'s contract).
    /// </summary>
    bool TryGet(SessionId sessionId, [NotNullWhen(true)] out ISessionQueue? session);

    /// <summary>
    /// Removes the session identified by <paramref name="sessionId"/>
    /// from the registry and disposes it. No-op when the session is
    /// not present. Async because session disposal involves draining a
    /// channel and tearing down a DI scope, both of which are async.
    /// </summary>
    ValueTask DeleteAsync(SessionId sessionId, CancellationToken cancellationToken);
}
