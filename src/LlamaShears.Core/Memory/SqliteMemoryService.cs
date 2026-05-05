using System.Buffers;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.Memory;

public sealed partial class SqliteMemoryService : IMemoryStore, IMemorySearcher, IMemoryIndexer
{
    private const string MemoryFolder = "memory";
    private const string IndexFolder = "system";
    private const string IndexFileName = ".memory.db";

    private readonly IAgentConfigProvider _configs;
    private readonly IEnumerable<IEmbeddingProviderFactory> _embeddingFactories;
    private readonly TimeProvider _time;
    private readonly MemoryServiceOptions _options;
    private readonly ILogger<SqliteMemoryService> _logger;

    public SqliteMemoryService(
        IAgentConfigProvider configs,
        IEnumerable<IEmbeddingProviderFactory> embeddingFactories,
        TimeProvider time,
        IOptions<MemoryServiceOptions> options,
        ILogger<SqliteMemoryService> logger)
    {
        _configs = configs;
        _embeddingFactories = embeddingFactories;
        _time = time;
        _options = options.Value;
        _logger = logger;
    }

    public async ValueTask<MemoryRef> StoreAsync(string agentId, string content, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentNullException.ThrowIfNull(content);

        var ctx = await ResolveAsync(agentId, cancellationToken).ConfigureAwait(false);
        var (relativePath, fullPath) = AllocateMemoryPath(ctx.WorkspaceRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        try
        {
            var hash = ComputeHash(content);
            var vector = await ctx.Embedding.EmbedAsync(content, EmbeddingPurpose.Document, cancellationToken).ConfigureAwait(false);
            await using var conn = OpenDb(ctx.IndexDbPath);
            EnsureSchema(conn);
            UpsertEntry(conn, relativePath, hash, vector);
            LogStored(_logger, agentId, relativePath);
        }
        catch (Exception ex) when (ex is SqliteException or InvalidOperationException or HttpRequestException)
        {
            // The file is the source of truth; the next reconcile picks
            // it up. Do not fail StoreAsync just because indexing didn't
            // land — surface the error to the log and move on.
            LogIndexingFailed(_logger, agentId, relativePath, ex.Message, ex);
        }

        return new MemoryRef(relativePath);
    }

    public async ValueTask<IReadOnlyList<MemorySearchResult>> SearchAsync(
        string agentId,
        string query,
        int limit,
        double minScore,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (limit <= 0)
        {
            return [];
        }

        var ctx = await ResolveAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (!File.Exists(ctx.IndexDbPath))
        {
            return [];
        }

        var queryVector = await ctx.Embedding.EmbedAsync(query, EmbeddingPurpose.Query, cancellationToken).ConfigureAwait(false);

        await using var conn = OpenDb(ctx.IndexDbPath);
        EnsureSchema(conn);

        var ranked = new List<MemorySearchResult>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT path, vector FROM memories";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var relativePath = reader.GetString(0);
                var bytes = (byte[])reader.GetValue(1);
                var vector = MemoryMarshal.Cast<byte, float>(bytes);
                var score = CosineSimilarity(queryVector.Span, vector);
                if (score < minScore)
                {
                    continue;
                }
                // Drop hits whose backing file is gone. Reconcile will
                // GC the orphaned row later; the model never sees it.
                if (!File.Exists(Path.Combine(ctx.WorkspaceRoot, relativePath)))
                {
                    continue;
                }
                ranked.Add(new MemorySearchResult(relativePath, score));
            }
        }

        ranked.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        if (ranked.Count > limit)
        {
            ranked.RemoveRange(limit, ranked.Count - limit);
        }
        return ranked;
    }

    public async ValueTask<MemoryReconciliation> ReconcileAsync(string agentId, bool force, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var ctx = await ResolveAsync(agentId, cancellationToken).ConfigureAwait(false);
        Directory.CreateDirectory(Path.GetDirectoryName(ctx.IndexDbPath)!);

        await using var conn = OpenDb(ctx.IndexDbPath);
        EnsureSchema(conn);

        var indexed = new Dictionary<string, string>(StringComparer.Ordinal);
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT path, hash FROM memories";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                indexed[reader.GetString(0)] = reader.GetString(1);
            }
        }

        var memoryRoot = Path.Combine(ctx.WorkspaceRoot, MemoryFolder);
        var added = 0;
        var updated = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        if (Directory.Exists(memoryRoot))
        {
            foreach (var fullPath in Directory.EnumerateFiles(memoryRoot, "*.md", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var relativePath = ToRelative(ctx.WorkspaceRoot, fullPath);
                seen.Add(relativePath);
                var content = await File.ReadAllTextAsync(fullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                var hash = ComputeHash(content);
                indexed.TryGetValue(relativePath, out var existingHash);
                var unchanged = existingHash is not null && string.Equals(existingHash, hash, StringComparison.Ordinal);
                if (unchanged && !force)
                {
                    continue;
                }
                var vector = await ctx.Embedding.EmbedAsync(content, EmbeddingPurpose.Document, cancellationToken).ConfigureAwait(false);
                UpsertEntry(conn, relativePath, hash, vector);
                if (existingHash is null)
                {
                    added++;
                }
                else
                {
                    updated++;
                }
            }
        }

        var orphans = new List<string>();
        foreach (var path in indexed.Keys)
        {
            if (!seen.Contains(path))
            {
                orphans.Add(path);
            }
        }
        foreach (var path in orphans)
        {
            DeleteEntry(conn, path);
        }
        return new MemoryReconciliation(Added: added, Updated: updated, Removed: orphans.Count);
    }

    private async ValueTask<MemoryContext> ResolveAsync(string agentId, CancellationToken cancellationToken)
    {
        var config = await _configs.GetConfigAsync(agentId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"No agent config for '{agentId}'.");
        if (string.IsNullOrEmpty(config.WorkspacePath))
        {
            throw new InvalidOperationException($"Agent '{agentId}' has no workspace path; memory tools require one.");
        }
        var workspace = Path.GetFullPath(config.WorkspacePath);
        var indexPath = Path.Combine(workspace, IndexFolder, IndexFileName);

        var embeddingId = config.Embedding?.Id ?? _options.DefaultEmbeddingModel
            ?? throw new InvalidOperationException(
                $"Agent '{agentId}' has no embedding model and no host-level default is configured.");
        var keepAlive = config.Embedding?.KeepAlive ?? _options.DefaultEmbeddingKeepAlive;
        var factory = _embeddingFactories.FirstOrDefault(f =>
            string.Equals(f.Name, embeddingId.Provider, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No embedding provider factory registered with name '{embeddingId.Provider}'.");
        var embedding = factory.CreateModel(new ModelConfiguration(
            ModelId: embeddingId.Model,
            KeepAlive: keepAlive));

        return new MemoryContext(workspace, indexPath, embedding);
    }

    private (string Relative, string Full) AllocateMemoryPath(string workspaceRoot)
    {
        var now = _time.GetUtcNow();
        var date = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var ts = now.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var dir = Path.Combine(workspaceRoot, MemoryFolder, date);
        var name = $"{ts}.md";
        var full = Path.Combine(dir, name);
        var counter = 1;
        while (File.Exists(full))
        {
            name = $"{ts}-{counter}.md";
            full = Path.Combine(dir, name);
            counter++;
        }
        return (ToRelative(workspaceRoot, full), full);
    }

    private static SqliteConnection OpenDb(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = true,
        }.ToString();
        var conn = new SqliteConnection(connectionString);
        conn.Open();
        return conn;
    }

    private static void EnsureSchema(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS memories (
                path TEXT PRIMARY KEY,
                hash TEXT NOT NULL,
                vector BLOB NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    private static void UpsertEntry(SqliteConnection conn, string path, string hash, ReadOnlyMemory<float> vector)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO memories (path, hash, vector)
            VALUES ($path, $hash, $vector)
            ON CONFLICT(path) DO UPDATE SET hash = excluded.hash, vector = excluded.vector;
            """;
        cmd.Parameters.AddWithValue("$path", path);
        cmd.Parameters.AddWithValue("$hash", hash);
        cmd.Parameters.AddWithValue("$vector", VectorToBytes(vector));
        cmd.ExecuteNonQuery();
    }

    private static void DeleteEntry(SqliteConnection conn, string path)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM memories WHERE path = $path";
        cmd.Parameters.AddWithValue("$path", path);
        cmd.ExecuteNonQuery();
    }

    private static byte[] VectorToBytes(ReadOnlyMemory<float> vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        MemoryMarshal.AsBytes(vector.Span).CopyTo(bytes);
        return bytes;
    }

    private static double CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }
        double dot = 0;
        double normA = 0;
        double normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        if (normA == 0 || normB == 0)
        {
            return 0;
        }
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static string ComputeHash(string content)
    {
        var byteCount = Encoding.UTF8.GetByteCount(content);
        var rented = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            var written = Encoding.UTF8.GetBytes(content, rented);
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(rented.AsSpan(0, written), hash);
            return Convert.ToHexStringLower(hash);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static string ToRelative(string workspaceRoot, string fullPath)
    {
        var rel = Path.GetRelativePath(workspaceRoot, fullPath);
        return rel.Replace(Path.DirectorySeparatorChar, '/');
    }

    private readonly record struct MemoryContext(string WorkspaceRoot, string IndexDbPath, IEmbeddingModel Embedding);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stored memory for agent '{AgentId}' at '{Path}'.")]
    private static partial void LogStored(ILogger logger, string agentId, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Indexing failed for agent '{AgentId}' memory '{Path}': {Message}")]
    private static partial void LogIndexingFailed(ILogger logger, string agentId, string path, string message, Exception ex);
}
