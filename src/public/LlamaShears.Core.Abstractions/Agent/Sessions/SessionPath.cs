namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Parent/root chain for an agent session. <see cref="Current"/> identifies this session;
/// <see cref="Parent"/> and <see cref="Root"/> identify the ancestor in the session tree. For
/// a root session all three refer to the same <see cref="SessionId"/>.
/// </summary>
/// <param name="Current">Session id of this session.</param>
/// <param name="Parent">Session id of this session's parent; equals <paramref name="Current"/> for a root.</param>
/// <param name="Root">Session id of the tree's root; equals <paramref name="Current"/> for a root.</param>
public sealed record SessionPath(SessionId Current, SessionId Parent, SessionId Root) : IAgentData
{
    /// <summary>Builds a root session path where current, parent, and root all reference <paramref name="current"/>.</summary>
    public SessionPath(SessionId current) : this(current, current, current)
    {
    }

    /// <summary>Guid of <see cref="Current"/>.</summary>
    public Guid Id => Current.Id;

    /// <summary>Guid of <see cref="Parent"/>.</summary>
    public Guid ParentId => Parent.Id;

    /// <summary>Guid of <see cref="Root"/>.</summary>
    public Guid RootId => Root.Id;

    /// <summary><see langword="true"/> when this path represents a root session (no parent above it).</summary>
    public bool IsRootSession => Id == RootId;

    /// <inheritdoc />
    public override string ToString()
    {
        if (Current != Root)
            return $"{Root}/{Parent}/{Current}";
        return $"{Current}";
    }

    /// <inheritdoc />
    public IEnumerable<KeyValuePair<string, object?>> GetData()
    {
        yield return new KeyValuePair<string, object?>(DataKey, this);
    }

    /// <summary>
    /// Creates a child session path using this path as the parent.
    /// </summary>
    /// <param name="session">Child session id.</param>
    /// <returns>Session path with the current instance as the parent and the same root.</returns>
    public SessionPath CreateChildSession(SessionId session)
    {
        return this with
        {
            Current = session,
            Parent = Current
        };
    }

    /// <summary>Key used to stash the active <see cref="SessionPath"/> in the per-turn data context scope.</summary>
    public const string DataKey = "session_path";
}
