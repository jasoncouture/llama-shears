namespace LlamaShears.Core.Abstractions.Provider;

public record ModelConfiguration(
    string ModelId,
    ThinkLevel Think = ThinkLevel.None,
    int? ContextLength = null,
    TimeSpan? KeepAlive = null,
    IReadOnlyDictionary<string, object>? Parameters = null,
    int TokenLimit = 0
);
