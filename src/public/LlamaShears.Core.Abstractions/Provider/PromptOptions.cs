using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Per-call overrides passed to <see cref="ILanguageModel.PromptAsync"/>.
/// <see langword="null"/> options means "use the model's configured
/// defaults verbatim".
/// </summary>
/// <param name="TokenLimit">Maximum response tokens for this call; <see langword="null"/> = use the configured limit.</param>
/// <param name="Tools">Tool groups visible to the model for this call; default = no tools.</param>
/// <param name="InjectEphemeralContext">When <see langword="true"/>, the inference runner renders the per-turn prompt-context template and inserts the resulting ephemeral turn into the prompt before dispatch; defaults to <see langword="false"/> so callers that want raw inference (e.g. compaction) keep their current behavior.</param>
/// <param name="EmitTurns">When <see langword="true"/>, the inference runner publishes the resulting Thought / Assistant <c>Turn</c> events after the stream completes; <see langword="false"/> for callers that consume the produced text directly without appending it to a conversation (e.g. compaction).</param>
public sealed record PromptOptions(
    int? TokenLimit = null,
    ImmutableArray<ToolGroup> Tools = default,
    bool InjectEphemeralContext = false,
    bool EmitTurns = false
);
