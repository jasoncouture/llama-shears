namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Plugin contract for an embedding provider, parallel to
/// <see cref="IProviderFactory"/>. A given provider may implement
/// chat (<see cref="IProviderFactory"/>), embeddings (this interface),
/// or both, registering each implemented contract separately into DI.
/// </summary>
public interface IEmbeddingProviderFactory
{
    /// <summary>
    /// Unique name of the provider. Same constraint as
    /// <see cref="IProviderFactory.Name"/>; the embedding factory and
    /// chat factory for the same provider share the name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Lists every embedding-capable model the provider surfaces, with
    /// metadata.
    /// </summary>
    IAsyncEnumerable<ModelInfo> ListModelsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Creates an embedding model from <paramref name="configuration"/>.
    /// </summary>
    IEmbeddingModel CreateModel(ModelConfiguration configuration);
}
