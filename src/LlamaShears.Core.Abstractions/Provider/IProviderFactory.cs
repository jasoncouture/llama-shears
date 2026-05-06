namespace LlamaShears.Core.Abstractions.Provider;

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
