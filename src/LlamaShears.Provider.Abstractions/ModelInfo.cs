namespace LlamaShears.Provider.Abstractions;

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
