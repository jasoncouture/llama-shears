namespace LlamaShears.Provider.Abstractions;

public record ModelConfiguration(
    string ModelId,
    IReadOnlyDictionary<string, object>? Parameters = null
);
