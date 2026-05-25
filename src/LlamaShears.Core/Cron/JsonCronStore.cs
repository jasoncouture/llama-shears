using System.Text.Json;
using System.Text.Json.Serialization;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Cron;

public sealed partial class JsonCronStore : ICronStore
{
    private const string CronFolderName = "cron";

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IApplicationPathProvider _paths;
    private readonly ILogger<JsonCronStore> _logger;
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
    private readonly Dictionary<string, Dictionary<Guid, CronJob>> _cache = new(StringComparer.Ordinal);

    public JsonCronStore(IApplicationPathProvider paths, ILogger<JsonCronStore> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    private string CronRoot => _paths.GetPath(PathKind.Data, CronFolderName, ensureExists: true);

    private string FilePathFor(string agentId) => Path.Combine(CronRoot, agentId + ".json");

    public async ValueTask<IReadOnlyList<CronJob>> GetAllAsync(string agentId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var bucket = await EnsureAgentLoadedAsync(agentId, cancellationToken);
            return [.. bucket.Values];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask<CronJob?> GetAsync(string agentId, Guid id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var bucket = await EnsureAgentLoadedAsync(agentId, cancellationToken);
            return bucket.GetValueOrDefault(id);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask UpsertAsync(CronJob job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentException.ThrowIfNullOrWhiteSpace(job.AgentId);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var bucket = await EnsureAgentLoadedAsync(job.AgentId, cancellationToken);
            bucket[job.Id] = job;
            await PersistAsync(job.AgentId, bucket, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask<bool> RemoveAsync(string agentId, Guid id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var bucket = await EnsureAgentLoadedAsync(agentId, cancellationToken);
            if (!bucket.Remove(id))
            {
                return false;
            }
            await PersistAsync(agentId, bucket, cancellationToken);
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<Guid, CronJob>> EnsureAgentLoadedAsync(string agentId, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(agentId, out var existing))
        {
            return existing;
        }

        var bucket = new Dictionary<Guid, CronJob>();
        var path = FilePathFor(agentId);
        if (File.Exists(path))
        {
            try
            {
                await using var stream = File.OpenRead(path);
                var loaded = await JsonSerializer
                    .DeserializeAsync<List<CronJob>>(stream, _jsonOptions, cancellationToken)
                    .ConfigureAwait(false) ?? [];
                foreach (var job in loaded)
                {
                    if (!string.Equals(job.AgentId, agentId, StringComparison.Ordinal))
                    {
                        LogAgentIdMismatch(path, job.Id, job.AgentId, agentId);
                        continue;
                    }
                    if (bucket.ContainsKey(job.Id))
                    {
                        LogDuplicateJobId(path, job.Id);
                    }
                    bucket[job.Id] = job;
                }
                LogLoaded(path, bucket.Count);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                LogLoadFailed(path, ex.Message, ex);
                bucket.Clear();
            }
        }

        _cache[agentId] = bucket;
        return bucket;
    }

    private async Task PersistAsync(string agentId, Dictionary<Guid, CronJob> bucket, CancellationToken cancellationToken)
    {
        var path = FilePathFor(agentId);
        if (bucket.Count == 0)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            return;
        }

        var temp = path + ".tmp";
        await using (var stream = File.Create(temp))
        {
            await JsonSerializer
                .SerializeAsync(stream, bucket.Values.OrderBy(j => j.CreatedAt).ToList(), _jsonOptions, cancellationToken);
        }
        File.Move(temp, path, overwrite: true);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {JobCount} cron job(s) from '{Path}'.")]
    private partial void LogLoaded(string path, int jobCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load cron store '{Path}': {Reason}. Starting empty.")]
    private partial void LogLoadFailed(string path, string reason, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cron store '{Path}' contains duplicate job id '{JobId}'; keeping the last occurrence.")]
    private partial void LogDuplicateJobId(string path, Guid jobId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cron store '{Path}' contains job '{JobId}' with AgentId '{StoredAgentId}' but the file is owned by '{ExpectedAgentId}'; skipping.")]
    private partial void LogAgentIdMismatch(string path, Guid jobId, string storedAgentId, string expectedAgentId);
}
