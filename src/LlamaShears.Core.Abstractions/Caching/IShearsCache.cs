namespace LlamaShears.Core.Abstractions.Caching;


/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IShearsCache<T> where T : class
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cacheKey"></param>
    /// <returns></returns>
    public CacheResult<TItem> TryGet<TItem>(string cacheKey);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cacheKey"></param>
    public void Invalidate(string cacheKey);
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    /// <param name="cacheKey"></param>
    /// <param name="value"></param>
    /// <param name="timeToLive"></param>
    public void Set<TItem>(string cacheKey, TItem value, TimeSpan timeToLive = default);
}
