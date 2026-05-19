using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Convenience accessors for pulling the active <see cref="SessionId"/> off
/// an <see cref="IDataContextScope"/> without callers having to remember the
/// well-known key.
/// </summary>
public static class SessionExtensions
{
    /// <summary>
    /// Returns the <see cref="SessionId"/> attached to the given scope under
    /// <see cref="SessionId.DataKey"/>, or <see langword="null"/> if none is set.
    /// </summary>
    /// <param name="scope">Data-context scope to inspect.</param>
    public static SessionId? TryGetSessionId(this IDataContextScope? scope)
    {
        if (scope is null) return null;
        scope.TryGetValue<SessionId>(SessionId.DataKey, out var session);
        return session;
    }

    /// <summary>
    /// Returns the <see cref="SessionId"/> attached to the given scope under
    /// <see cref="SessionId.DataKey"/>. Throws when the scope is
    /// <see langword="null"/> or has no session stashed.
    /// </summary>
    /// <param name="scope">Data-context scope to inspect.</param>
    public static SessionId GetSessionId(this IDataContextScope? scope)
    {
        var session = scope.TryGetSessionId() ?? throw new InvalidOperationException($"Tried to get current session from {SessionId.DataKey}, but no session was found");
        return session;
    }
}
