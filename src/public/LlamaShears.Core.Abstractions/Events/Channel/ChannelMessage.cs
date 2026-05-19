using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Core.Abstractions.Events.Channel;

/// <summary>
/// One message inbound on a chat channel. Routed onto the event bus so
/// the agent loop, UI, and any audit subscribers see the same payload.
/// </summary>
/// <param name="Text">User-supplied text.</param>
/// <param name="AgentId">Target agent id when the message is addressed to a specific agent; <see langword="null"/> for broadcast/system messages.</param>
/// <param name="Timestamp">When the message was received.</param>
public record ChannelMessage(string Text, string? AgentId, DateTimeOffset Timestamp)
{
    /// <summary>Non-text payloads (e.g. images) attached to this message.</summary>
    public ImmutableArray<Attachment> Attachments { get; init; } = [];

    /// <summary>
    /// Sender's session id when the message originates from a non-default
    /// session (e.g. an ephemeral child session replying to its parent agent).
    /// <see langword="null"/> for default-session senders — user chat, slash
    /// commands, host-side injections. Receivers ignore this field today;
    /// it exists as audit/UI metadata and as the seam for future
    /// session-aware routing.
    /// </summary>
    public Guid? SessionId { get; init; }
}
