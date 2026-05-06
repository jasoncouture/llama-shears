using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Speaker role attached to a <see cref="ModelTurn"/>. Distinguishes
/// genuine user/assistant traffic from framework-injected scaffolding
/// and from hidden reasoning that must be filtered out before the
/// turn is sent back to the model.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ModelRole>))]
public enum ModelRole
{
    /// <summary>The model's persistent system prompt (typically one per conversation).</summary>
    System,
    /// <summary>A user-authored turn.</summary>
    User,
    /// <summary>An assistant turn produced by the model.</summary>
    Assistant,
    /// <summary>A user-authored turn injected by the framework (heartbeat, system signals).</summary>
    FrameworkUser,
    /// <summary>An assistant turn injected by the framework (e.g. compaction summaries).</summary>
    FrameworkAssistant,
    /// <summary>Hidden chain-of-thought emitted by a thinking-capable model. Never resubmitted to the model.</summary>
    Thought,
    /// <summary>Tool-call result fed back into the conversation.</summary>
    Tool,
    /// <summary>Per-turn ephemeral system context appended to the next user turn rather than persisted as a separate turn.</summary>
    SystemEphemeral,
}
