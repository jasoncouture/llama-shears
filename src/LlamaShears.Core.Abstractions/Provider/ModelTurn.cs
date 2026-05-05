using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Events;

namespace LlamaShears.Core.Abstractions.Provider;

public record ModelTurn(
    ModelRole Role,
    string Content,
    DateTimeOffset Timestamp,
    string? ChannelId = null) : IContextEntry
{
    public ImmutableArray<ToolCall> ToolCalls { get; init; } = [];

    public string? ToolName { get; init; }
}
