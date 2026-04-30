namespace LlamaShears.Core.Abstractions.Provider;

public record ModelInfo(
    string ModelId,
    string DisplayName,
    string? Description,
    SupportedInputType SupportedInputs,
    bool SupportsReasoning,
    int MaxContextWindow
);
