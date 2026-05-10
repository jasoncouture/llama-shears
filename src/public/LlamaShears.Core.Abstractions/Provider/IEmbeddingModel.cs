namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Provider-agnostic seam for generating embeddings. Parallel to
/// <see cref="ILanguageModel"/>; an underlying provider may implement
/// chat, embeddings, or both. Implementations send the supplied text
/// to their underlying API verbatim — any model-specific decoration
/// (asymmetric query/document prefixes, normalization) is the caller's
/// responsibility, configured separately.
/// </summary>
public interface IEmbeddingModel
{
    /// <summary>
    /// Embeds <paramref name="text"/> and returns its vector.
    /// Dimensionality is determined by the model and is reflected in
    /// the length of the returned memory.
    /// </summary>
    ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken);
}
