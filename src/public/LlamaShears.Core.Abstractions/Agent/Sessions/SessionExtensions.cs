using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Convenience accessors for pulling the active <see cref="SessionPath"/> off
/// an <see cref="IDataContextScope"/> without callers having to remember the
/// well-known key.
/// </summary>
public static class SessionExtensions
{
    /// <summary>
    /// Returns the <see cref="SessionPath"/> stashed on <paramref name="scope"/> under
    /// <see cref="SessionPath.DataKey"/>, or <see langword="null"/> when the scope is
    /// <see langword="null"/> or has no path set.
    /// </summary>
    public static SessionPath? TryGetSessionPath(this IDataContextScope? scope)
    {
        if (scope is null) return null;
        scope.TryGetValue<SessionPath>(SessionPath.DataKey, out var sessionPath);
        return sessionPath;
    }

    /// <summary>
    /// Returns the <see cref="SessionPath"/> stashed on <paramref name="scope"/>. Throws when the
    /// scope is <see langword="null"/> or has no path set.
    /// </summary>
    public static SessionPath GetSessionPath(this IDataContextScope? scope)
    {
        var session = scope.TryGetSessionPath() ?? throw new InvalidOperationException($"Tried to get current session path from {SessionPath.DataKey}, but no session was found");
        return session;
    }

    /// <summary>Returns the <see cref="SessionPath.Current"/> session id for the scope.</summary>
    public static SessionId GetCurrentSessionId(this IDataContextScope? scope)
    {
        return scope.GetSessionPath().Current;
    }

    /// <summary>Returns the <see cref="SessionPath.Parent"/> session id for the scope.</summary>
    public static SessionId GetParentSessionId(this IDataContextScope? scope)
    {
        return scope.GetSessionPath().Parent;
    }

    /// <summary>Returns the <see cref="SessionPath.Root"/> session id for the scope.</summary>
    public static SessionId GetRootSessionId(this IDataContextScope? scope)
    {
        return scope.GetSessionPath().Root;
    }

    /// <summary><see langword="true"/> when the scope's session path is a root session (no parent above it).</summary>
    public static bool IsRootSession(this IDataContextScope? scope)
    {
        return scope.TryGetSessionPath()?.IsRootSession == true;
    }
}
