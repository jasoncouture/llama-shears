using System.ComponentModel.DataAnnotations;

using LlamaShears.Core.Abstractions.Common;
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
    /// Unique name of the provider; the embedding factory and chat factory
    /// for the same provider share the name. Compared case-insensitively
    /// against <see cref="CompositeIdentity.Provider"/> when routing.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Lists every embedding-capable model the provider surfaces, with
    /// metadata.
    /// </summary>
    IAsyncEnumerable<ModelInfo> ListModelsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Asks the provider to validate <paramref name="configuration"/>. Today
    /// the only check is that the model identified by
    /// <see cref="ModelConfiguration.Id"/> exists in the provider's
    /// catalogue; the contract is shaped so future implementations can
    /// surface additional reasons (token-limit ceilings, parameter
    /// compatibility, etc.) without an interface change.
    /// </summary>
    /// <param name="configuration">Configuration to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <see cref="ValidationResult.Success"/> (i.e. <see langword="null"/>) when
    /// the configuration is valid; otherwise a populated
    /// <see cref="ValidationResult"/> whose <see cref="ValidationResult.ErrorMessage"/>
    /// explains the failure.
    /// </returns>
    ValueTask<ValidationResult?> ValidateAsync(ModelConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>
    /// Creates an embedding model from <paramref name="configuration"/>.
    /// </summary>
    IEmbeddingModel CreateModel(ModelConfiguration configuration);
}
