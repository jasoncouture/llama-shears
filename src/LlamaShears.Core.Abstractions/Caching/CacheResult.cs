namespace LlamaShears.Core.Abstractions.Caching;

public record CacheResult<TItem>(
    bool Present,
    bool TypeMismatch = false,
    TItem? Value = default) where TItem : class;
