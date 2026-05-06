using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Core.Abstractions.Events.Channel;

public record ChannelMessage(string Text, string? AgentId, DateTimeOffset Timestamp)
{
    public ImmutableArray<Attachment> Attachments { get; init; } = [];
}
