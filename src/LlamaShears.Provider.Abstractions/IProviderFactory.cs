namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Factory for creating model providers. The Name property must match the pattern: ^[A-Z]([A-Z0-9-_]*)[A-Z0-9]+$
/// (First character A-Z, alphanumeric plus - and _ in the middle, alphanumeric at the end.)
/// If the name does not match, the provider will be ignored.
/// </summary>
public interface IProviderFactory
{
    /// <summary>
    /// The unique name of the provider factory. Must match ^[A-Z]([A-Z0-9-_]*)[A-Z0-9]+$
    /// </summary>
    string Name { get; }

    /// <summary>
    /// List all models surfaced by this provider, with metadata.
    /// </summary>
    IAsyncEnumerable<ModelInfo> ListModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a model instance from the given configuration.
    /// </summary>
    ILanguageModel CreateModel(ModelConfiguration configuration);
}
