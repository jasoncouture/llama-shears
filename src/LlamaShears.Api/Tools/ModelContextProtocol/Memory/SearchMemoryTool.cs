using System.Collections.Immutable;
using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Memory;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

[McpServerToolType]
public sealed partial class SearchMemoryTool
{
    private const int DefaultLimit = 10;
    private const int HardMaxLimit = 100;
    private const double DefaultMinScore = 0.30;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IMemorySearcher _searcher;
    private readonly ILogger<SearchMemoryTool> _logger;

    public SearchMemoryTool(IAgentWorkspaceLocator workspace, IMemorySearcher searcher, ILogger<SearchMemoryTool> logger)
    {
        _workspace = workspace;
        _searcher = searcher;
        _logger = logger;
    }

    [McpServerTool(Name = "memory_search")]
    [Description("Vector-searches the agent's memory index and returns the top matching memories as a JSON object: echoes the query/minScore/limit, plus a hitCount and an array of hits (workspace-relative path, cosine similarity score, first-line summary). Read full bodies on demand with file_read. The hits array is empty when nothing crosses minScore.")]
    public async Task<MemorySearchResultPayload> SearchMemory(
        [Description("Free-text query. Embedded with the agent's configured embedding model and compared by cosine similarity.")] string query,
        [Description("Maximum number of hits to return. Defaults to 10; hard-capped at 100.")] int limit = DefaultLimit,
        [Description("Minimum cosine similarity (0.0 - 1.0). Hits below this score are dropped. Defaults to 0.30 — relevant matches typically land 0.40-0.60 with task-prefixed asymmetric encoders, noise stays under 0.10, so 0.30 sits safely in the gap. Don't raise above ~0.55 unless you want very tight matches only.")] double minScore = DefaultMinScore,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        var cap = Math.Clamp(limit, 1, HardMaxLimit);
        var floor = Math.Clamp(minScore, 0.0, 1.0);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return Failure(query ?? string.Empty, floor, cap, "Refused: search_memory requires an authenticated agent on the request.");
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            return Failure(query ?? string.Empty, floor, cap, "Refused: query is required.");
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
            return Failure(query, floor, cap, $"Refused: {ex.Message}");
        }
    }

    private static MemorySearchResultPayload Failure(string query, double minScore, int limit, string error)
        => new(
            Query: query,
            MinScore: minScore,
            Limit: limit,
            HitCount: 0,
            Hits: [],
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' searched memory: '{Query}' → {Hits} hits.")]
    private partial void LogSearched(string agentId, string query, int hits);

    [LoggerMessage(Level = LogLevel.Warning, Message = "search_memory failed for agent '{AgentId}': {Message}")]
    private partial void LogSearchFailed(string agentId, string message, Exception ex);
}
