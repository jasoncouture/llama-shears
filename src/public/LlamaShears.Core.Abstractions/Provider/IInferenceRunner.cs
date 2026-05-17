namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streams a single model prompt, emits per-fragment events, and
/// optionally emits the resulting Thought / Assistant turn events.
/// Lifts the inference loop out of the context compactor and the
/// agent so both can share it; the language model, event-id, and
/// correlation-id used for the call and the published events are all
/// resolved from the ambient agent scope, so callers set state once
/// on the data scope before invoking the runner instead of threading
/// it through every call.
/// </summary>
public interface IInferenceRunner
{
    /// <summary>
    /// Runs <paramref name="prompt"/> through the scope's language
    /// model and publishes message/thought fragment events keyed at
    /// the ambient agent state's event id. When
    /// <see cref="PromptOptions.EmitTurns"/> is <see langword="true"/>,
    /// also publishes a <c>Turn(Thought)</c> event (if any thinking
    /// arrived) and a <c>Turn(Assistant)</c> event (if any content
    /// arrived) — callers like the compactor leave it at
    /// <see langword="false"/> when the produced text is consumed
    /// directly rather than appended to a conversation.
    /// </summary>
    Task<InferenceOutcome> RunAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        CancellationToken cancellationToken);
}
