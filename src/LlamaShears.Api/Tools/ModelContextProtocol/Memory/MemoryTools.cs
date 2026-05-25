using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Memory;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

[McpServerToolType]
public sealed partial class MemoryTools
{
    private const int DefaultSearchLimit = 10;
    private const int HardMaxSearchLimit = 100;
    private const double DefaultMinScore = 0.30;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IMemoryStore _store;
    private readonly IMemorySearcher _searcher;
    private readonly IMemoryIndexer _indexer;
    private readonly ILogger<MemoryTools> _logger;

    public MemoryTools(
        IAgentWorkspaceLocator workspace,
        IMemoryStore store,
        IMemorySearcher searcher,
        IMemoryIndexer indexer,
        ILogger<MemoryTools> logger)
    {
        _workspace = workspace;
        _store = store;
        _searcher = searcher;
        _indexer = indexer;
        _logger = logger;
    }

    [McpServerTool(Name = "memory_store", Destructive = false, OpenWorld = false)]
    [Description("Stores a memory file in the agent's workspace under memory/YYYY-MM-DD/<unix-seconds>.md. Returns a JSON object with the stored flag and the workspace-relative path of the new memory file. The file is the source of truth and is also embedded into the agent's vector index so memory_search can find it. Indexing failures do not fail the write — the next memory_index will catch up. Convention: lead the file with a single-line summary (typically a markdown H1 like '# Short title — what this memory says'). When this memory matches a future turn, only that first line is auto-injected into context; the full body is fetched on demand via file_read. A weak first line means a weak summary, so the rest of the memory may never get loaded.")]
    public async Task<MemoryStoreResult> StoreMemory(
        [Description("Memory content (markdown). Stored verbatim. The first line is surfaced as the memory's summary in injected context — make it a meaningful one-line description.")] string content,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return StoreFailure("Refused: store_memory requires an authenticated agent on the request.");
        }
        if (string.IsNullOrEmpty(content))
        {
            return StoreFailure("Refused: content is required.");
        }

        try
        {
            var memoryRef = await _store.StoreAsync(workspace.AgentId, content, cancellationToken);
            LogStored(workspace.AgentId, memoryRef.RelativePath);
            return new MemoryStoreResult(
                Stored: true,
                RelativePath: memoryRef.RelativePath);
        }
        catch (InvalidOperationException ex)
        {
            LogStoreFailed(workspace.AgentId, ex.Message, ex);
            return StoreFailure($"Refused: {ex.Message}");
        }
    }

    [McpServerTool(Name = "memory_search", Destructive = false, OpenWorld = false, ReadOnly = true)]
    [Description("Vector-searches the agent's memory index and returns the top matching memories as a JSON object: echoes the query/minScore/limit, plus a hitCount and an array of hits (workspace-relative path, cosine similarity score, first-line summary). Read full bodies on demand with file_read. The hits array is empty when nothing crosses minScore.")]
    public async Task<MemorySearchResultPayload> SearchMemory(
        [Description("Free-text query. Embedded with the agent's configured embedding model and compared by cosine similarity.")] string query,
        [Description("Maximum number of hits to return. Defaults to 10; hard-capped at 100.")] int limit = DefaultSearchLimit,
        [Description("Minimum cosine similarity (0.0 - 1.0). Hits below this score are dropped. Defaults to 0.30 — relevant matches typically land 0.40-0.60 with task-prefixed asymmetric encoders, noise stays under 0.10, so 0.30 sits safely in the gap. Don't raise above ~0.55 unless you want very tight matches only.")] double minScore = DefaultMinScore,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        var cap = Math.Clamp(limit, 1, HardMaxSearchLimit);
        var floor = Math.Clamp(minScore, 0.0, 1.0);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return SearchFailure(query ?? string.Empty, floor, cap, "Refused: search_memory requires an authenticated agent on the request.");
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            return SearchFailure(query ?? string.Empty, floor, cap, "Refused: query is required.");
        }

        try
        {
            var hits = await _searcher.SearchAsync(workspace.AgentId, query, cap, floor, cancellationToken);
            LogSearched(workspace.AgentId, query, hits.Count);
            var payload = ImmutableArray.CreateBuilder<MemorySearchHit>();
            foreach (var hit in hits)
            {
                payload.Add(new MemorySearchHit(
                    RelativePath: hit.RelativePath,
                    Score: hit.Score,
                    Summary: hit.Summary ?? string.Empty));
            }
            return new MemorySearchResultPayload(
                Query: query,
                MinScore: floor,
                Limit: cap,
                HitCount: payload.Count,
                Hits: payload.ToImmutable());
        }
        catch (InvalidOperationException ex)
        {
            LogSearchFailed(workspace.AgentId, ex.Message, ex);
            return SearchFailure(query, floor, cap, $"Refused: {ex.Message}");
        }
    }

    [McpServerTool(Name = "memory_index", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Forces a full reconcile of the agent's memory index against the filesystem. Returns a JSON object with the reconciled flag, added/updated/removed/total counts, and elapsedMilliseconds. Pass force=true to re-embed every file regardless of whether its content has changed — use this if the embedding model or its prompt convention has changed and old vectors need rebuilding.")]
    public async Task<MemoryIndexResult> IndexMemory(
        [Description("If true, re-embed every file even when its content hash already matches the indexed hash. Defaults to false.")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return IndexFailure("Refused: memory_index requires an authenticated agent on the request.");
        }

        try
        {
            var startedAt = Stopwatch.GetTimestamp();
            var summary = await _indexer.ReconcileAsync(workspace.AgentId, force, cancellationToken);
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            LogReconciled(workspace.AgentId, summary.Added, summary.Updated, summary.Removed, summary.Total, elapsedMs);
            return new MemoryIndexResult(
                Reconciled: true,
                Added: summary.Added,
                Updated: summary.Updated,
                Removed: summary.Removed,
                Total: summary.Total,
                ElapsedMilliseconds: elapsedMs);
        }
        catch (InvalidOperationException ex)
        {
            LogReconcileFailed(workspace.AgentId, ex.Message, ex);
            return IndexFailure($"Refused: {ex.Message}");
        }
    }

    private static MemoryStoreResult StoreFailure(string error)
        => new(
            Stored: false,
            RelativePath: null,
            Error: error);

    private static MemorySearchResultPayload SearchFailure(string query, double minScore, int limit, string error)
        => new(
            Query: query,
            MinScore: minScore,
            Limit: limit,
            HitCount: 0,
            Hits: [],
            Error: error);

    private static MemoryIndexResult IndexFailure(string error)
        => new(
            Reconciled: false,
            Added: 0,
            Updated: 0,
            Removed: 0,
            Total: 0,
            ElapsedMilliseconds: 0,
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' stored memory '{Path}'.")]
    private partial void LogStored(string agentId, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "store_memory failed for agent '{AgentId}': {Message}")]
    private partial void LogStoreFailed(string agentId, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' searched memory: '{Query}' → {Hits} hits.")]
    private partial void LogSearched(string agentId, string query, int hits);

    [LoggerMessage(Level = LogLevel.Warning, Message = "search_memory failed for agent '{AgentId}': {Message}")]
    private partial void LogSearchFailed(string agentId, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' reconciled memory index: +{Added} ~{Updated} -{Removed}, {Total} total, elapsed={ElapsedMs:F2}ms.")]
    private partial void LogReconciled(string agentId, int added, int updated, int removed, int total, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "index_memory failed for agent '{AgentId}': {Message}")]
    private partial void LogReconcileFailed(string agentId, string message, Exception ex);
}
