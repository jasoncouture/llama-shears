namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Provider-agnostic seam for invoking a language model. Implementations
/// own the wire format and lifecycle of the underlying model; callers
/// see only the streaming response of <see cref="IModelResponseFragment"/>
/// values.
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// Streams the model's response to <paramref name="prompt"/> as a
    /// sequence of fragments. The enumeration completes when the model
    /// has finished; cancellation aborts the in-flight request. The
    /// final fragment is always an <see cref="IModelCompletionResponse"/>
    /// when the provider can report token usage.
    /// </summary>
    IAsyncEnumerable<IModelResponseFragment> PromptAsync(ModelPrompt prompt, CancellationToken cancellationToken);

    /// <summary>
    /// Returns an upper-bound token estimate for <paramref name="turn"/>.
    /// Implementations that can reach a real tokenizer should override
    /// to return a tight count (still favoring over-estimation when the
    /// chat-template wrap is unknown). The default is a coarse
    /// character-based heuristic intended only as a safe fallback —
    /// never returns less than the actual cost.
    /// </summary>
    ValueTask<int> EstimateAsync(ModelTurn turn, CancellationToken cancellationToken)
    {
        // length * 1.5 / 2 — pessimistic for English BPE (~3x real),
        // pessimistic enough for code/Unicode (~1.5x real). Ceiling so
        // single-character turns still register as ≥ 1.
        var estimate = (int)Math.Ceiling(turn.Content.Length * 1.5 / 2.0);
        return ValueTask.FromResult(estimate);
    }
}
