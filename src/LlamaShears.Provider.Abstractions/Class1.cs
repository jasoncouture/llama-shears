/// <summary>
/// Configuration for creating a model instance.
/// </summary>
public record ModelConfiguration(
	string ModelId,
	IReadOnlyDictionary<string, object>? Parameters = null
);

/// <summary>
/// Base interface for all language models.
/// </summary>
public interface ILanguageModel
{
	// The core interface for all model interactions will be defined here.
}
namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Types of input a model may support.
/// </summary>
[Flags]
public enum SupportedInputType
{
	None = 0,
	Text = 1 << 0,
	Image = 1 << 1,
	Audio = 1 << 2,
	Video = 1 << 3
}

/// <summary>
/// Metadata describing a model surfaced by a provider.
/// </summary>
public record ModelInfo(
	string ModelId,
	string DisplayName,
	string? Description,
	SupportedInputType SupportedInputs,
	bool SupportsReasoning,
	int MaxContextWindow
);

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
