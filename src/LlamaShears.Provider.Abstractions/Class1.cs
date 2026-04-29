namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Factory for creating model providers. The Name property must match the pattern: ^[A-Z]([A-Z0-9-_]*)[A-Z0-9]+$
/// (First character A-Z, alphanumeric plus - and _ in the middle, alphanumeric at the end.)
/// </summary>
public interface IProviderFactory
{
	/// <summary>
	/// The unique name of the provider factory. Must match ^[A-Z]([A-Z0-9-_]*)[A-Z0-9]+$
	/// </summary>
	string Name { get; }
}
