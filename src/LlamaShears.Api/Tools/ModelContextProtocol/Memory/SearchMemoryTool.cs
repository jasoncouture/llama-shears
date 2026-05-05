using System.ComponentModel;
using System.Globalization;
using System.Text;
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
    [Description("Vector-searches the agent's memory index and returns the top matching file paths (workspace-relative) with similarity scores. Read the bodies on demand with file_read. Returns an empty list when nothing crosses min_score.")]
    public async Task<string> SearchMemory(
        [Description("Free-text query. Embedded with the agent's configured embedding model and compared by cosine similarity.")] string query,
        [Description("Maximum number of hits to return. Defaults to 10; hard-capped at 100.")] int limit = DefaultLimit,
        [Description("Minimum cosine similarity (0.0 - 1.0). Hits below this score are dropped. Defaults to 0.30 — relevant matches typically land 0.40-0.60 with task-prefixed asymmetric encoders, noise stays under 0.10, so 0.30 sits safely in the gap. Don't raise above ~0.55 unless you want very tight matches only.")] double minScore = DefaultMinScore,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return "Refused: search_memory requires an authenticated agent on the request.";
        }
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Refused: query is required.";
        }

        var cap = Math.Clamp(limit, 1, HardMaxLimit);
        var floor = Math.Clamp(minScore, 0.0, 1.0);

        try
        {
            var hits = await _searcher.SearchAsync(workspace.AgentId, query, cap, floor, cancellationToken).ConfigureAwait(false);
            LogSearched(_logger, workspace.AgentId, query, hits.Count);
            return Render(query, hits, floor, cap);
        }
        catch (InvalidOperationException ex)
        {
            LogSearchFailed(_logger, workspace.AgentId, ex.Message, ex);
            return $"Refused: {ex.Message}";
        }
    }

    private static string Render(string query, IReadOnlyList<MemorySearchResult> hits, double minScore, int limit)
    {
        if (hits.Count == 0)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "No memories matched '{0}' at min_score={1:F2} (limit={2}).",
                query,
                minScore,
                limit);
        }

        var builder = new StringBuilder();
        builder.AppendFormat(
            CultureInfo.InvariantCulture,
            "{0} match{1} for '{2}' (min_score={3:F2}, limit={4}):",
            hits.Count,
            hits.Count == 1 ? string.Empty : "es",
            query,
            minScore,
            limit);
        foreach (var hit in hits)
        {
            builder.Append('\n');
            builder.AppendFormat(CultureInfo.InvariantCulture, "{0:F4}  {1}", hit.Score, hit.RelativePath);
        }
        return builder.ToString();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' searched memory: '{Query}' → {Hits} hits.")]
    private static partial void LogSearched(ILogger logger, string agentId, string query, int hits);

    [LoggerMessage(Level = LogLevel.Warning, Message = "search_memory failed for agent '{AgentId}': {Message}")]
    private static partial void LogSearchFailed(ILogger logger, string agentId, string message, Exception ex);
}
