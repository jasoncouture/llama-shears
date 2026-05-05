using System.Buffers;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqliteVec;

namespace LlamaShears.Core.Memory;

public sealed partial class SqliteMemoryService : IMemoryStore, IMemorySearcher, IMemoryIndexer
{
    private const string MemoryFolder = "memory";
    private const string IndexFolder = "system";
    private const string IndexFileName = ".memory.db";
    private const string CollectionName = "memories";

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
            var vector = await ctx.Embedding.EmbedAsync($"{ctx.DocumentPrefix}{content}", cancellationToken).ConfigureAwait(false);
            var collection = await OpenCollectionAsync(ctx, vector.Length, cancellationToken).ConfigureAwait(false);
            await collection.UpsertAsync(
                new MemoryVectorRecord { Path = relativePath, Hash = hash, Vector = vector },
                cancellationToken).ConfigureAwait(false);
            LogStored(_logger, agentId, relativePath);
        }
        catch (Exception ex) when (ex is VectorStoreException or InvalidOperationException or HttpRequestException)
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

        var queryVector = await ctx.Embedding.EmbedAsync($"{ctx.QueryPrefix}{query}", cancellationToken).ConfigureAwait(false);
        var collection = await OpenCollectionAsync(ctx, queryVector.Length, cancellationToken).ConfigureAwait(false);

        var ranked = new List<MemorySearchResult>();
        var scanned = 0;
        var topRawScore = double.NegativeInfinity;
        await foreach (var hit in collection
            .SearchAsync(queryVector, top: Math.Max(limit * 4, limit), cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            scanned++;
            // SqliteVec returns cosine *distance* (lower is better; 0 =
            // identical) in VectorSearchResult.Score. Translate to the
            // similarity scale callers expect (1.0 = identical).
            var score = 1.0 - hit.Score ?? 0.0;
            if (score > topRawScore)
            {
                topRawScore = score;
            }
            if (score < minScore)
            {
                continue;
            }
            // Drop hits whose backing file is gone. Reconcile will GC
            // the orphaned row later; the model never sees it.
            var relativePath = hit.Record.Path;
            if (!File.Exists(Path.Combine(ctx.WorkspaceRoot, relativePath)))
            {
                continue;
            }
            ranked.Add(new MemorySearchResult(relativePath, score));
        }

        ranked.Sort(static (a, b) => b.Score.CompareTo(a.Score));
        LogSearchScored(_logger, agentId, scanned, ranked.Count, topRawScore == double.NegativeInfinity ? 0 : topRawScore, minScore);
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

        try
        {
            return await ReconcileCoreAsync(agentId, force, ctx, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (IsVectorDimensionMismatch(ex))
        {
            // The agent's embedding model produces a different vector
            // dimension than the existing index was built for. sqlite-vec
            // surfaces this as a NOT NULL constraint failure on the
            // vector column. Drop the index and rebuild from disk — the
            // markdown files are the source of truth.
            LogIndexSchemaMismatchRebuilding(_logger, agentId, ctx.IndexDbPath);
            ResetIndex(ctx.IndexDbPath);
            return await ReconcileCoreAsync(agentId, force: true, ctx, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<MemoryReconciliation> ReconcileCoreAsync(
        string agentId,
        bool force,
        MemoryContext ctx,
        CancellationToken cancellationToken)
    {
        // Probe the embedder once so we know the dimension before
        // touching the collection — needed even when no on-disk files
        // exist (orphan-only reconcile case).
        var dimension = await ProbeDimensionAsync(ctx, cancellationToken).ConfigureAwait(false);
        var collection = await OpenCollectionAsync(ctx, dimension, cancellationToken).ConfigureAwait(false);

        var indexed = new Dictionary<string, string>(StringComparer.Ordinal);
        await foreach (var record in collection
            .GetAsync(static _ => true, top: int.MaxValue, cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            indexed[record.Path] = record.Hash;
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
                var vector = await ctx.Embedding.EmbedAsync($"{ctx.DocumentPrefix}{content}", cancellationToken).ConfigureAwait(false);
                await collection.UpsertAsync(
                    new MemoryVectorRecord { Path = relativePath, Hash = hash, Vector = vector },
                    cancellationToken).ConfigureAwait(false);
                if (existingHash is null)
                {
                    added++;
                    LogIndexedAdded(_logger, agentId, relativePath);
                }
                else
                {
                    updated++;
                    LogIndexedUpdated(_logger, agentId, relativePath, force);
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
            await collection.DeleteAsync(path, cancellationToken).ConfigureAwait(false);
            LogIndexedRemoved(_logger, agentId, path);
        }
        return new MemoryReconciliation(Added: added, Updated: updated, Removed: orphans.Count, Total: seen.Count);
    }

    private static bool IsVectorDimensionMismatch(Exception exception)
    {
        for (var ex = exception; ex is not null; ex = ex.InnerException!)
        {
            if (ex is SqliteException sqlite)
            {
                var msg = sqlite.Message;
                // sqlite-vec's native dim check, fires on a properly-
                // shaped vec0 table when an upsert vector size disagrees
                // with the column's declared FLOAT[N]. Manifests as
                // SQLITE_ERROR with a "Dimension mismatch" message.
                if (msg.Contains("Dimension mismatch", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                // Legacy hand-rolled schema (pre-VectorData migration)
                // being read by sqlite-vec. The underlying memories
                // table doesn't match what vec0 expects, so the upsert
                // surfaces as a NOT NULL violation on the vector column
                // (SQLITE_CONSTRAINT, error code 19).
                if (sqlite.SqliteErrorCode == 19
                    && msg.Contains($"{CollectionName}.vector", StringComparison.Ordinal))
                {
                    return true;
                }
            }
            if (ex.InnerException is null)
            {
                break;
            }
        }
        return false;
    }

    private static void ResetIndex(string indexDbPath)
    {
        // Microsoft.Data.Sqlite caches connections per connection-string
        // in a process-wide pool. Clear it before deleting the file or
        // Windows refuses the unlink (Linux happily lets a held inode
        // be unlinked, so this is mostly a Windows guard, but harmless
        // either way).
        SqliteConnection.ClearAllPools();
        if (File.Exists(indexDbPath))
        {
            File.Delete(indexDbPath);
        }
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
        var queryPrefix = config.Embedding?.QueryPrefix ?? _options.DefaultEmbeddingQueryPrefix ?? string.Empty;
        var documentPrefix = config.Embedding?.DocumentPrefix ?? _options.DefaultEmbeddingDocumentPrefix ?? string.Empty;
        var factory = _embeddingFactories.FirstOrDefault(f =>
            string.Equals(f.Name, embeddingId.Provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No embedding provider factory registered with name '{embeddingId.Provider}'.");
        var embedding = factory.CreateModel(new ModelConfiguration(
            ModelId: embeddingId.Model,
            KeepAlive: keepAlive,
            AgentOptions: config.Embedding?.Options));

        return new MemoryContext(workspace, indexPath, embedding, queryPrefix, documentPrefix);
    }

    private async ValueTask<VectorStoreCollection<string, MemoryVectorRecord>> OpenCollectionAsync(
        MemoryContext ctx,
        int dimensions,
        CancellationToken cancellationToken)
    {
        var dir = Path.GetDirectoryName(ctx.IndexDbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var connectionString = $"Data Source={ctx.IndexDbPath};Pooling=True";
        var definition = new VectorStoreCollectionDefinition
        {
            Properties =
            [
                new VectorStoreKeyProperty(nameof(MemoryVectorRecord.Path), typeof(string)),
                new VectorStoreDataProperty(nameof(MemoryVectorRecord.Hash), typeof(string)),
                new VectorStoreVectorProperty(nameof(MemoryVectorRecord.Vector), typeof(ReadOnlyMemory<float>), dimensions)
                {
                    DistanceFunction = DistanceFunction.CosineDistance,
                },
            ],
        };
        var store = new SqliteVectorStore(connectionString);
        var collection = store.GetCollection<string, MemoryVectorRecord>(CollectionName, definition);
        await collection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
        return collection;
    }

    private static async ValueTask<int> ProbeDimensionAsync(MemoryContext ctx, CancellationToken cancellationToken)
    {
        var probe = await ctx.Embedding.EmbedAsync($"{ctx.DocumentPrefix}probe", cancellationToken).ConfigureAwait(false);
        return probe.Length;
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

    private readonly record struct MemoryContext(
        string WorkspaceRoot,
        string IndexDbPath,
        IEmbeddingModel Embedding,
        string QueryPrefix,
        string DocumentPrefix);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stored memory for agent '{AgentId}' at '{Path}'.")]
    private static partial void LogStored(ILogger logger, string agentId, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Indexing failed for agent '{AgentId}' memory '{Path}': {Message}")]
    private static partial void LogIndexingFailed(ILogger logger, string agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Memory search for agent '{AgentId}': scanned={Scanned}, hits={Hits}, top-raw-score={TopScore:F4}, min-score={MinScore:F2}.")]
    private static partial void LogSearchScored(ILogger logger, string agentId, int scanned, int hits, double topScore, double minScore);

    [LoggerMessage(Level = LogLevel.Information, Message = "Memory indexed (added) for agent '{AgentId}': {Path}")]
    private static partial void LogIndexedAdded(ILogger logger, string agentId, string path);

    [LoggerMessage(Level = LogLevel.Information, Message = "Memory indexed (updated, force={Force}) for agent '{AgentId}': {Path}")]
    private static partial void LogIndexedUpdated(ILogger logger, string agentId, string path, bool force);

    [LoggerMessage(Level = LogLevel.Information, Message = "Memory indexed (removed orphan) for agent '{AgentId}': {Path}")]
    private static partial void LogIndexedRemoved(ILogger logger, string agentId, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Memory index for agent '{AgentId}' has a vector dimension mismatch (likely an embedding-model change); resetting '{Path}' and rebuilding from disk.")]
    private static partial void LogIndexSchemaMismatchRebuilding(ILogger logger, string agentId, string path);
}
