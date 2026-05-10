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
    /// Unique name of the provider factory. Must match
    /// <c>^[A-Z]([A-Z0-9-_]*)[A-Z0-9]+$</c>; factories with a non-matching
    /// name are ignored.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Lists every model the provider surfaces, with metadata.
    /// </summary>
    IAsyncEnumerable<ModelInfo> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a model instance from <paramref name="configuration"/>.
    /// </summary>
    ILanguageModel CreateModel(ModelConfiguration configuration);
}
