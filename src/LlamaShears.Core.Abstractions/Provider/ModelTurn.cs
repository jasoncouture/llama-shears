using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Events;

namespace LlamaShears.Core.Abstractions.Provider;

public record ModelTurn(
    ModelRole Role,
    string Content,
    DateTimeOffset Timestamp,
    string? ChannelId = null) : IContextEntry
{
    public ImmutableArray<ToolCall> ToolCalls { get; init; } = [];

    public ToolCall? ToolCall { get; init; }

    public bool IsError { get; init; }

    public ImmutableArray<Attachment> Attachments { get; init; } = [];
}
