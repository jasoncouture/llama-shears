namespace LlamaShears.Provider.Abstractions;

public record ModelInfo(
    string ModelId,
    string DisplayName,
    string? Description,
    SupportedInputType SupportedInputs,
    bool SupportsReasoning,
    int MaxContextWindow
);
