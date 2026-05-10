using System.ComponentModel.DataAnnotations;

using LlamaShears.Core.Abstractions.Common;
namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Plugin contract for a language-model provider. Surfaces the catalog
/// of models the provider can serve and constructs
/// <see cref="ILanguageModel"/> instances from
/// <see cref="ModelConfiguration"/>. One factory per provider (Ollama,
/// future cloud providers, etc.).
/// </summary>
public interface IProviderFactory
{
    /// <summary>
    /// Unique name of the provider factory. Compared case-insensitively
    /// against <see cref="CompositeIdentity.Provider"/> when routing.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Lists every model the provider surfaces, with metadata.
    /// </summary>
    IAsyncEnumerable<ModelInfo> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the provider to validate <paramref name="configuration"/>. Today
    /// the only check is that the model identified by
    /// <see cref="ModelConfiguration.ModelId"/> exists in the provider's
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
    /// Creates a model instance from <paramref name="configuration"/>.
    /// </summary>
    ILanguageModel CreateModel(ModelConfiguration configuration);
}
