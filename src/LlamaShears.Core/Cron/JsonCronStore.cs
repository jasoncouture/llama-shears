using System.Text.Json;
using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Cron;

public sealed partial class JsonCronStore : ICronStore
{
    private const string FileName = "cron.json";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IShearsPaths _paths;
    private readonly ILogger<JsonCronStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Dictionary<Guid, CronJob>? _cache;

    public JsonCronStore(IShearsPaths paths, ILogger<JsonCronStore> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    private string FilePath => Path.Combine(
        _paths.GetPath(PathKind.Data, ensureExists: true),
        FileName);

    public async ValueTask<IReadOnlyList<CronJob>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return [.. _cache!.Values];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask<CronJob?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            return _cache!.GetValueOrDefault(id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask UpsertAsync(CronJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            _cache![job.Id] = job;
            await PersistAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask<bool> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
            if (!_cache!.Remove(id))
            {
                return false;
            }
            await PersistAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return;
        }

        var path = FilePath;
        if (!File.Exists(path))
        {
            _cache = [];
            return;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var loaded = await JsonSerializer
                .DeserializeAsync<List<CronJob>>(stream, _jsonOptions, cancellationToken)
                .ConfigureAwait(false) ?? [];
            _cache = loaded.ToDictionary(j => j.Id);
            LogLoaded(_logger, path, _cache.Count);
        }
        catch (JsonException ex)
        {
            LogLoadFailed(_logger, path, ex.Message, ex);
            _cache = [];
        }
    }

    private async Task PersistAsync(CancellationToken cancellationToken)
    {
        var path = FilePath;
        var temp = path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer
                .SerializeAsync(stream, _cache!.Values.OrderBy(j => j.CreatedAt).ToList(), _jsonOptions, cancellationToken)
                .ConfigureAwait(false);
        }
        File.Move(temp, path, overwrite: true);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {JobCount} cron job(s) from '{Path}'.")]
    private static partial void LogLoaded(ILogger logger, string path, int jobCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to deserialize cron store '{Path}': {Reason}. Starting empty.")]
    private static partial void LogLoadFailed(ILogger logger, string path, string reason, Exception ex);
}
