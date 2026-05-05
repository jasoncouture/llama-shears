namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Provider-agnostic seam for generating embeddings. Parallel to
/// <see cref="ILanguageModel"/>; an underlying provider may implement
/// chat, embeddings, or both.
/// </summary>
public interface IEmbeddingModel
{
    /// <summary>
    /// Embeds <paramref name="text"/> and returns its vector.
    /// Dimensionality is determined by the model and is reflected in
    /// the length of the returned memory.
    /// </summary>
    ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken);

    /// <summary>
    /// Batched form. The result has the same length and order as
    /// <paramref name="texts"/>. An empty input yields an empty result.
    /// </summary>
    ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
