using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Convenience accessors for pulling the active <see cref="SessionId"/> off
/// an <see cref="IDataContextScope"/> without callers having to remember the
/// well-known key.
/// </summary>
public static class SessionExtensions
{
    /// <param name="scope">Data-context scope to inspect.</param>
    extension(IDataContextScope? scope)
    {
        /// <summary>
        /// Returns the <see cref="SessionId"/> attached to the given scope under
        /// <see cref="SessionId.DataKey"/>, or <see langword="null"/> if none is set.
        /// </summary>
        public SessionId? TryGetSessionId()
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
        public SessionId GetSessionId()
        {
            var session = scope.TryGetSessionId() ?? throw new InvalidOperationException($"Tried to get current session from {SessionId.DataKey}, but no session was found");
            return session;
        }
    }
}
