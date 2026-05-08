using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Content;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// One entry in an agent's conversation log. Carries the speaker role,
/// body text, and any tool-call or attachment metadata associated with
/// the turn.
/// </summary>
/// <param name="Role">Who/what authored this turn.</param>
/// <param name="Content">Body text of the turn.</param>
/// <param name="Timestamp">When the turn was recorded.</param>
/// <param name="ChannelId">Channel correlation id for routing turns back into a multi-channel UI; <see langword="null"/> when the turn has no channel context.</param>
/// <param name="Ephemeral">
/// When <see langword="true"/>, the turn is transient: subscribers may
/// observe it (e.g. UI streaming) but persisters skip it. Drives the
/// "don't record this" decision from the turn itself instead of from a
/// central filter.
/// </param>
public record ModelTurn(
    ModelRole Role,
    string Content,
    DateTimeOffset Timestamp,
    string? ChannelId = null,
    bool Ephemeral = false) : IContextEntry
{
    /// <summary>Tool calls the model emitted on this turn (assistant turns only).</summary>
    public ImmutableArray<ToolCall> ToolCalls { get; init; } = [];

    /// <summary>The tool call this turn is the result of (tool turns only); <see langword="null"/> otherwise.</summary>
    public ToolCall? ToolCall { get; init; }

    /// <summary>True when this turn represents a failed tool call or framework-level error.</summary>
    public bool IsError { get; init; }

    /// <summary>Non-text payloads attached to this turn (images, etc.).</summary>
    public ImmutableArray<Attachment> Attachments { get; init; } = [];
}
