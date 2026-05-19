namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Live state for an ephemeral session, stashed in the session's data
/// context scope under <see cref="DataKey"/>. The session's
/// <c>session_reply</c> tool reads <see cref="Parent"/> /
/// <see cref="ChannelId"/> / <see cref="SessionId"/> to publish back to
/// the parent and flips <see cref="ReplySent"/> so the owning session
/// knows the fallback path is not needed.
/// </summary>
public sealed class EphemeralSessionContext
{
    /// <summary>Data-context key for the active <see cref="EphemeralSessionContext"/>.</summary>
    public const string DataKey = "ephemeral_session_context";

    /// <summary>Parent session this ephemeral child sends its reply to.</summary>
    public required EphemeralSessionReference Parent { get; init; }

    /// <summary>This ephemeral session's own id; stamped onto outbound payloads.</summary>
    public required Guid SessionId { get; init; }

    /// <summary>Effective channel id used for ModelTurn tagging and the session_reply event suffix.</summary>
    public required string ChannelId { get; init; }

    /// <summary>
    /// Set by the <c>session_reply</c> tool when it successfully
    /// publishes. Read by the owning <c>IEphemeralSession</c> after the
    /// loop exits to decide whether the fallback path should fire.
    /// </summary>
    public bool ReplySent { get; set; }
}
