namespace LlamaShears.Provider.Abstractions;

/// <summary>
/// Configuration for creating a model instance.
/// </summary>
public record ModelConfiguration(
    string ModelId,
    IReadOnlyDictionary<string, object>? Parameters = null
);
