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
    /// has finished; cancellation aborts the in-flight request.
    /// </summary>
    IAsyncEnumerable<IModelResponseFragment> PromptAsync(ModelPrompt prompt, CancellationToken cancellationToken);
}
