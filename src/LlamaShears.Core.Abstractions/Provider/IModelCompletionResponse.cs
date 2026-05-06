namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Final fragment in a model response carrying metadata about the
/// completed turn. Emitted exactly once, after every text and thought
/// fragment, so callers can react to completion-time information
/// (token usage, etc.) without needing a separate signal.
/// </summary>
public interface IModelCompletionResponse : IModelResponseFragment
{
    /// <summary>
    /// Total tokens consumed by the conversation through the end of
    /// this turn — typically <c>prompt_tokens + response_tokens</c>.
    /// Providers without a server-side count may return a generous
    /// estimate; the value is intended as an upper bound for context
    /// budgeting, never an under-count.
    /// </summary>
    int TokenCount { get; }
}
