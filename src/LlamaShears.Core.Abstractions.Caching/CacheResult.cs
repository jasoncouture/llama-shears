namespace LlamaShears.Core.Abstractions.Caching;

/// <summary>
/// Outcome of <see cref="IShearsCache{T}.TryGet{TItem}"/>: distinguishes
/// "no entry", "entry exists under a different type", and "entry hit"
/// so callers can branch without exception handling.
/// </summary>
/// <typeparam name="TItem">Type the caller asked the cache to interpret the entry as.</typeparam>
/// <param name="Present">Whether an entry was found and matched <typeparamref name="TItem"/>.</param>
/// <param name="TypeMismatch">Whether an entry existed at the key but its cached value was a different type than <typeparamref name="TItem"/>.</param>
/// <param name="Value">The cached value when <paramref name="Present"/> is <see langword="true"/>; otherwise <see langword="default"/>.</param>
public record CacheResult<TItem>(
    bool Present,
    bool TypeMismatch = false,
    TItem? Value = default) where TItem : class;
