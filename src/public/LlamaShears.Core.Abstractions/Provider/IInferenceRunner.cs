namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streams a single model prompt, emits per-fragment events, and
/// optionally emits the resulting Thought / Assistant turn events.
/// Lifts the inference loop out of the context compactor and the
/// agent so both can share it; the event-id and correlation-id used
/// for published events are read from the ambient agent state, so
/// callers set those once on the data scope before invoking the
/// runner instead of threading them through every call.
/// </summary>
public interface IInferenceRunner
{
    /// <summary>
    /// Runs <paramref name="prompt"/> through <paramref name="model"/>
    /// and publishes message/thought fragment events keyed at the
    /// ambient agent state's event id. When <see cref="PromptOptions.EmitTurns"/>
    /// is <see langword="true"/>, also publishes a <c>Turn(Thought)</c>
    /// event (if any thinking arrived) and a <c>Turn(Assistant)</c>
    /// event (if any content arrived) — callers like the compactor
    /// leave it at <see langword="false"/> when the produced text is
    /// consumed directly rather than appended to a conversation.
    /// </summary>
    Task<InferenceOutcome> RunAsync(
        ILanguageModel model,
        ModelPrompt prompt,
        PromptOptions? options,
        CancellationToken cancellationToken);
}
