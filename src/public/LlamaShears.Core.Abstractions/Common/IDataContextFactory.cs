using System.Diagnostics.CodeAnalysis;
using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Common;

/// <summary>
/// Manages keyed <see cref="IDataContextScope"/> instances flowing on
/// the current call chain via <see cref="AsyncLocal{T}"/>.
/// Other call chains looking up the same key can rejoin via
/// <see cref="TryJoinContextScope"/>.
/// </summary>
public interface IDataContextFactory
{
    /// <summary>The scope active on the current call chain, or <see langword="null"/>.</summary>
    IDataContextScope? Current { get; set; }

    /// <summary>
    /// Joins an existing scope identified by <paramref name="sessionId"/> as
    /// the current call chain's active scope. Throws if the call chain
    /// already has a different scope. Returns <see langword="false"/>
    /// when no scope with that key is alive.
    /// </summary>
    bool TryJoinContextScope(SessionId sessionId, [NotNullWhen(true)] out IDataContextScope? context);
    
    /// <summary>
    /// Creates a new empty scope keyed by <paramref name="sessionId"/> and sets it
    /// as the current call chain's active scope. Throws when a live scope
    /// already claims that key. The returned scope must be populated via
    /// <see cref="InitializeAsync"/> before consumers read from it.
    /// </summary>
    IDataContextScope CreateContext(SessionId sessionId);

    /// <summary>
    /// Populates the scope keyed by <paramref name="sessionId"/>: <paramref name="values"/>
    /// are written first (so providers can observe them), singleton item
    /// providers contribute next (only when the scope is otherwise empty),
    /// then call-site scoped providers (<paramref name="scopeProviders"/>)
    /// contribute on top.
    /// </summary>
    ValueTask InitializeAsync(SessionId sessionId, IEnumerable<IDataContextItemProvider> scopeProviders,
        IEnumerable<KeyValuePair<string, object?>> values, CancellationToken cancellationToken);

    /// <summary>Forcibly removes the scope keyed by <paramref name="sessionId"/>.</summary>
    void DeleteContext(SessionId sessionId);

    /// <summary>
    /// Detaches the current scope from this call chain. When
    /// <paramref name="owner"/> is <see langword="true"/> the underlying
    /// scope is also removed from the factory's registry.
    /// </summary>
    void ClearCurrent(bool owner = false);
}
