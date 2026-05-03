using LlamaShears.Core.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace LlamaShears.Core.Caching;

public sealed class ShearsCache<T> : IShearsCache<T> where T : class
{
    private static readonly string _keyPrefix = $"{typeof(T).FullName ?? typeof(T).Name}:";

    private readonly IMemoryCache _cache;

    public ShearsCache(IMemoryCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);
        _cache = cache;
    }

    private static string ToScopedCacheKey(string key) => $"{_keyPrefix}{key}";

    public CacheResult<TItem> TryGet<TItem>(string cacheKey) where TItem : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);

        if (!_cache.TryGetValue(ToScopedCacheKey(cacheKey), out var raw))
        {
            return new CacheResult<TItem>(Present: false);
        }

        if (raw is null)
        {
            return new CacheResult<TItem>(Present: true, TypeMismatch: false);
        }

        if (raw is not TItem typed)
        {
            return new CacheResult<TItem>(Present: true, TypeMismatch: true);
        }

        return new CacheResult<TItem>(Present: true, TypeMismatch: false, Value: typed);
    }



    public void Invalidate(string cacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        _cache.Remove(ToScopedCacheKey(cacheKey));
    }

    public void Set<TItem>(string cacheKey, TItem? value, TimeSpan timeToLive) where TItem : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        if (timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeToLive),
                timeToLive,
                "TimeSpan must be strictly positive; zero or negative TTLs are not supported.");
        }

        _cache.Set(
            ToScopedCacheKey(cacheKey),
            value,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = timeToLive,
            });
    }
}
