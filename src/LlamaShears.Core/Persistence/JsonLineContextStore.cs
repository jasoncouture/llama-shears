using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Persistence;

public sealed class JsonLineContextStore : IContextStore
{
    private const string DefaultCurrentFileName = "current.json";

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IApplicationPathProvider _paths;
    private readonly TimeProvider _time;
    private readonly ConcurrentDictionary<SessionKey, AgentContext> _contexts =
        new ConcurrentDictionary<SessionKey, AgentContext>();

    public JsonLineContextStore(IApplicationPathProvider paths, TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(time);

        _paths = paths;
        _time = time;
    }

    public async Task<IAgentContext> OpenAsync(SessionId session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var key = SessionKey.For(session);
        if (_contexts.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var currentPath = ResolveCurrentPath(session, ensureFolderExists: true);

        var seed = new List<IContextEntry>();
        await foreach (var entry in ReadJsonLinesAsync(currentPath, cancellationToken).ConfigureAwait(false))
        {
            seed.Add(entry);
        }

        var fresh = new AgentContext(session, currentPath, seed, _jsonOptions);
        return _contexts.GetOrAdd(key, fresh);
    }

    public IAsyncEnumerable<IContextEntry> ReadCurrentAsync(SessionId session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        var currentPath = ResolveCurrentPath(session, ensureFolderExists: false);
        return ReadJsonLinesAsync(currentPath, cancellationToken);
    }

    public IAsyncEnumerable<IContextEntry> ReadArchiveAsync(ArchiveId archiveId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archiveId.Session);
        var folder = ResolveFolder(archiveId.Session, ensureExists: false);
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

    public Task<IReadOnlyList<ArchiveId>> ListArchivesAsync(SessionId session, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var folder = ResolveFolder(session, ensureExists: false);
        if (!Directory.Exists(folder))
        {
            return Task.FromResult<IReadOnlyList<ArchiveId>>([]);
        }

        var archives = new List<ArchiveId>();
        foreach (var path in Directory.EnumerateFiles(folder, "*.json"))
        {
            var name = Path.GetFileNameWithoutExtension(path);
            if (IsCurrentFileName(session, name))
            {
                continue;
            }

            if (long.TryParse(name, out var unixMillis))
            {
                archives.Add(new ArchiveId(session, unixMillis));
            }
        }
        archives.Sort((a, b) => a.UnixMillis.CompareTo(b.UnixMillis));
        return Task.FromResult<IReadOnlyList<ArchiveId>>(archives);
    }

    public Task ClearAsync(SessionId session, bool archive, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);

        var folder = ResolveFolder(session, ensureExists: true);
        var currentPath = Path.Combine(folder, ResolveCurrentFileName(session));

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

        if (_contexts.TryGetValue(SessionKey.For(session), out var context))
        {
            context.ClearInMemory();
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(ArchiveId archiveId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(archiveId.Session);

        var folder = ResolveFolder(archiveId.Session, ensureExists: false);
        var archivePath = Path.Combine(folder, $"{archiveId.UnixMillis}.json");
        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }
        return Task.CompletedTask;
    }

    private string ResolveFolder(SessionId session, bool ensureExists)
    {
        return session.IsDefault
            ? _paths.GetPath(PathKind.Context, session.AgentId, ensureExists: ensureExists)
            : _paths.GetPath(PathKind.Context, Path.Combine(session.AgentId, session.Name), ensureExists: ensureExists);
    }

    private string ResolveCurrentPath(SessionId session, bool ensureFolderExists)
    {
        var folder = ResolveFolder(session, ensureExists: ensureFolderExists);
        return Path.Combine(folder, ResolveCurrentFileName(session));
    }

    private static string ResolveCurrentFileName(SessionId session)
        => session.IsDefault ? DefaultCurrentFileName : $"{session.Id:n}.json";

    private static bool IsCurrentFileName(SessionId session, string nameWithoutExtension)
        => session.IsDefault
            ? string.Equals(nameWithoutExtension, "current", StringComparison.Ordinal)
            : string.Equals(nameWithoutExtension, session.Id.ToString("n"), StringComparison.Ordinal);

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

    private readonly record struct SessionKey(string AgentId, string Name, Guid? Id)
    {
        public static SessionKey For(SessionId session)
            => session.IsDefault
                ? new SessionKey(session.AgentId, session.Name, null)
                : new SessionKey(session.AgentId, session.Name, session.Id);
    }
}
