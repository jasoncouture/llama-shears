namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streams a single model prompt, emits per-fragment events, and
/// optionally emits the resulting Thought / Assistant turn events.
/// Lifts the inference loop out of <see cref="IContextCompactor"/>
/// and the agent so both can share it; the <c>eventId</c> parameter
/// is the third segment of the published <c>EventType</c> and lets
/// observers tell agent traffic apart from compaction traffic.
/// </summary>
public interface IInferenceRunner
{
    /// <summary>
    /// Runs <paramref name="prompt"/> through <paramref name="model"/>
    /// and publishes message/thought fragment events keyed at
    /// <paramref name="eventId"/>. When <paramref name="emitTurns"/> is
    /// <see langword="true"/>, also publishes a <c>Turn(Thought)</c>
    /// event (if any thinking arrived) and a <c>Turn(Assistant)</c>
    /// event (if any content arrived) — callers like the compactor
    /// pass <see langword="false"/> when the produced text is consumed
    /// directly rather than appended to a conversation.
    /// </summary>
    Task<InferenceOutcome> RunAsync(
        string eventId,
        ILanguageModel model,
        ModelPrompt prompt,
        PromptOptions? options,
        bool emitTurns,
        Guid correlationId,
        CancellationToken cancellationToken);
}
