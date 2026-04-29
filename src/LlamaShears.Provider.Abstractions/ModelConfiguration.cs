namespace LlamaShears.Provider.Abstractions;

public record ModelConfiguration(
    string ModelId,
    ThinkLevel Think = ThinkLevel.None,
    int? ContextLength = null,
    TimeSpan? KeepAlive = null,
    IReadOnlyDictionary<string, object>? Parameters = null
);
