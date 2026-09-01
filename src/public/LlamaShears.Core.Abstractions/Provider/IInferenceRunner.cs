using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Streams a single model prompt, emits per-fragment events, and
/// optionally emits the resulting Thought / Assistant turn events.
/// Collects tool calls the model emitted but does not dispatch them.
/// Lifts the inference loop out of the context compactor and the
/// agent so both can share it. Callers pass the session,
/// correlation, and channel used to key published events; the
/// runner does not read them from the prompt or the data scope.
/// </summary>
public interface IInferenceRunner
{
    /// <summary>
    /// Runs <paramref name="prompt"/> through the scope's language
    /// model and publishes message/thought fragment events keyed at
    /// <paramref name="sessionId"/>. When
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
        SessionId sessionId,
        Guid correlationId,
        string? channelId,
        CancellationToken cancellationToken);
}
