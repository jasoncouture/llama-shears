using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using LlamaShears.Agent.Abstractions.Persistence;
using LlamaShears.Hosting;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.Agent.Core.Persistence;

public sealed class JsonLineContextStore : IContextStore
{
    private const string CurrentFileName = "current.json";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IShearsPaths _paths;
    private readonly TimeProvider _time;
    private readonly ConcurrentDictionary<string, AgentContext> _contexts = new(StringComparer.Ordinal);

    public JsonLineContextStore(IShearsPaths paths, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(time);

        _paths = paths;
        _time = time;
    }

    public async Task<IAgentContext> OpenAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        if (_contexts.TryGetValue(agentId, out var existing))
        {
            return existing;
        }

        var folder = _paths.GetPath(PathKind.Context, agentId, ensureExists: true);
        var currentPath = Path.Combine(folder, CurrentFileName);

        var seed = new List<IContextEntry>();
        await foreach (var entry in ReadJsonLinesAsync(currentPath, cancellationToken).ConfigureAwait(false))
        {
            seed.Add(entry);
        }

        var fresh = new AgentContext(agentId, currentPath, seed, _jsonOptions);
        return _contexts.GetOrAdd(agentId, fresh);
    }

    public IAsyncEnumerable<IContextEntry> ReadCurrentAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        var folder = _paths.GetPath(PathKind.Context, agentId);
        var currentPath = Path.Combine(folder, CurrentFileName);
        return ReadJsonLinesAsync(currentPath, cancellationToken);
    }

    public IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveId.AgentId);
        var folder = _paths.GetPath(PathKind.Context, archiveId.AgentId);
        var archivePath = Path.Combine(folder, $"{archiveId.UnixMillis}.json");
        return ReadJsonLinesAsync(archivePath, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ListAgentsAsync(CancellationToken cancellationToken)
    {
        var root = _paths.GetPath(PathKind.Context);
        if (!Directory.Exists(root))
        {
            return Task.FromResult<IReadOnlyList<string>>([]);
        }

        var agents = Directory.EnumerateDirectories(root)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(agents);
    }

    public Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var folder = _paths.GetPath(PathKind.Context, agentId);
        if (!Directory.Exists(folder))
        {
            return Task.FromResult<IReadOnlyList<ArchiveId>>([]);
        }

        var archives = new List<ArchiveId>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(name, "current", StringComparison.Ordinal))
            {
                continue;
            }

            if (long.TryParse(name, out var unixMillis))
            {
                archives.Add(new ArchiveId(agentId, unixMillis));
            }
        }
        archives.Sort((a, b) => a.UnixMillis.CompareTo(b.UnixMillis));
        return Task.FromResult<IReadOnlyList<ArchiveId>>(archives);
    }

    public Task ClearAsync(string agentId, bool archive, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var folder = _paths.GetPath(PathKind.Context, agentId, ensureExists: true);
        var currentPath = Path.Combine(folder, CurrentFileName);

        if (File.Exists(currentPath))
        {
            if (archive)
            {
                var archivePath = Path.Combine(folder, $"{_time.GetUtcNow().ToUnixTimeMilliseconds()}.json");
                File.Move(currentPath, archivePath);
            }
            else
            {
                File.Delete(currentPath);
            }
        }

        if (_contexts.TryGetValue(agentId, out var context))
        {
            context.ClearInMemory();
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(archiveId.AgentId);

        var folder = _paths.GetPath(PathKind.Context, archiveId.AgentId);
        var archivePath = Path.Combine(folder, $"{archiveId.UnixMillis}.json");
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }
        return Task.CompletedTask;
    }

    private static async IAsyncEnumerable<IContextEntry> ReadJsonLinesAsync(
        string path,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            yield break;
        }

        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            IContextEntry? entry;
            try
            {
                entry = JsonSerializer.Deserialize<IContextEntry>(line, _jsonOptions);
            }
            catch (JsonException)
            {
                continue;
            }

            if (entry is not null)
            {
                yield return entry;
            }
        }
    }
}
