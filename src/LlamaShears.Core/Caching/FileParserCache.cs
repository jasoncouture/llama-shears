using LlamaShears.Core.Abstractions.Caching;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.Caching;

public sealed class FileParserCache<T> : IFileParserCache<T>, IDisposable where T : class
{
    private readonly IShearsCache<T> _cache;
    private readonly IDisposable? _monitorRegistration;
    private long _timeToLiveTicks;

    public FileParserCache(IShearsCache<T> cache, IOptionsMonitor<FileParserCacheOptions> options)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        _cache = cache;
        _monitorRegistration = options.OnChange(Apply);
        Apply(options.CurrentValue);
    }

    public async ValueTask<TItem?> GetOrParseAsync<TItem, TState>(
        string path,
        TState state,
        Func<Stream?, TState, CancellationToken, ValueTask<TItem?>> parser,
        CancellationToken cancellationToken)
        where TItem : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(parser);

        var info = new FileInfo(path);
        var key = BuildKey<TItem>(path, info);

        var hit = _cache.TryGet<TItem>(key);
        if (hit.Present && !hit.TypeMismatch)
        {
            return hit.Value;
        }

        TItem? result;
        if (info.Exists)
        {
            await using var stream = info.OpenRead();
            result = await parser.Invoke(stream, state, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            result = await parser.Invoke(null, state, cancellationToken).ConfigureAwait(false);
        }

        _cache.Set(key, result, TimeToLive);
        return result;
    }

    public void Dispose() => _monitorRegistration?.Dispose();

    private TimeSpan TimeToLive => TimeSpan.FromTicks(Interlocked.Read(ref _timeToLiveTicks));

    private void Apply(FileParserCacheOptions opts)
    {
        var ticks = opts.TimeToLive.Ticks;
        if (ticks > 0)
        {
            Interlocked.Exchange(ref _timeToLiveTicks, ticks);
        }
    }

    private static string BuildKey<TItem>(string path, FileInfo info)
    {
        var prefix = typeof(TItem).FullName ?? typeof(TItem).Name;
        return info.Exists
            ? $"{prefix}:{path}:1:{info.LastWriteTimeUtc.Ticks}:{info.Length}"
            : $"{prefix}:{path}:0::";
    }
}
