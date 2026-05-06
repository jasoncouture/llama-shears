namespace LlamaShears.Core.Abstractions.Caching;

/// <summary>
/// Per-owner view onto the host's shared in-memory cache. The type
/// parameter <typeparamref name="T"/> identifies the consumer (typically
/// the calling class, mirroring the <c>ILogger&lt;T&gt;</c> pattern); its
/// full type name is automatically prefixed onto every key so consumers
/// cannot collide with each other's keyspaces by accident.
/// <para>
/// Entries are constrained to reference types: most structs do not carry
/// enough state to warrant caching, and constraining the cached item
/// type to <see langword="class"/> avoids boxing and the
/// <see cref="Nullable{T}"/> wrapping that would otherwise be needed to
/// express "not present" cleanly.
/// </para>
/// <para>
/// Time-to-live is absolute: an entry stored with a TTL of <c>X</c> is
/// evicted <c>X</c> after <see cref="Set{TItem}"/> returns, regardless of
/// reads. Sliding behaviour is up to the caller — re-call
/// <see cref="Set{TItem}"/> on hit to refresh.
/// </para>
/// </summary>
/// <typeparam name="T">The owning type. Used as the key prefix.</typeparam>
public interface IShearsCache<T> where T : class
{
    /// <summary>
    /// Looks up the entry at <paramref name="cacheKey"/> for this owner.
    /// The returned <see cref="CacheResult{TItem}"/> distinguishes "no
    /// entry", "entry exists but the cached value was a different type"
    /// (<see cref="CacheResult{TItem}.TypeMismatch"/>), and "entry exists
    /// and matches" — none are errors, so callers branch on the result
    /// without exception handling.
    /// </summary>
    public CacheResult<TItem> TryGet<TItem>(string cacheKey) where TItem : class;

    /// <summary>
    /// Removes the entry at <paramref name="cacheKey"/> for this owner if
    /// one exists. No-op when there is no matching entry.
    /// </summary>
    public void Invalidate(string cacheKey);

    /// <summary>
    /// Stores <paramref name="value"/> at <paramref name="cacheKey"/> for
    /// this owner with an absolute time-to-live of
    /// <paramref name="timeToLive"/>. Replaces any existing entry at the
    /// same key. Throws if <paramref name="timeToLive"/> is not strictly
    /// positive — that path would either expire on insert or persist for
    /// the lifetime of the cache, neither of which this API supports by
    /// design.
    /// </summary>
    public void Set<TItem>(string cacheKey, TItem? value, TimeSpan timeToLive) where TItem : class;
}
