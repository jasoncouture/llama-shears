using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Core.Abstractions.Events.Channel;

/// <summary>
/// Inbound channel message routed to a specific session. Published on
/// <see cref="Event.WellKnown.Channel.Message"/> with the channel id in
/// the <c>Id</c> segment of the event type.
/// </summary>
/// <param name="Text">User-supplied body of the message.</param>
/// <param name="ChannelId">Channel the message originated on (e.g. <c>webui</c>, <c>telegram:123</c>).</param>
/// <param name="Timestamp">When the message was produced.</param>
public record ChannelMessage(string Text, string ChannelId, DateTimeOffset Timestamp)
{
    /// <summary>Non-text payloads (e.g. images) attached to this message.</summary>
    public ImmutableArray<Attachment> Attachments { get; init; } = [];
}
