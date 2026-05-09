using System.ComponentModel;
using System.Diagnostics;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Memory;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

[McpServerToolType]
public sealed partial class IndexMemoryTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IMemoryIndexer _indexer;
    private readonly ILogger<IndexMemoryTool> _logger;

    public IndexMemoryTool(IAgentWorkspaceLocator workspace, IMemoryIndexer indexer, ILogger<IndexMemoryTool> logger)
    {
        _workspace = workspace;
        _indexer = indexer;
        _logger = logger;
    }

    [McpServerTool(Name = "memory_index")]
    [Description("Forces a full reconcile of the agent's memory index against the filesystem. Adds new files, re-embeds changed ones, and removes orphaned index entries. Reports the counts. Pass force=true to re-embed every file regardless of whether its content has changed — use this if the embedding model or its prompt convention has changed and old vectors need rebuilding.")]
    public async Task<string> IndexMemory(
        [Description("If true, re-embed every file even when its content hash already matches the indexed hash. Defaults to false.")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return "Refused: memory_index requires an authenticated agent on the request.";
        }

        try
        {
            var startedAt = Stopwatch.GetTimestamp();
            var summary = await _indexer.ReconcileAsync(workspace.AgentId, force, cancellationToken).ConfigureAwait(false);
            var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;
            LogReconciled(_logger, workspace.AgentId, summary.Added, summary.Updated, summary.Removed, summary.Total, elapsedMs);
            return $"Reconciled memory index: {summary.Added} added, {summary.Updated} updated, {summary.Removed} removed, {summary.Total} total.";
        }
        catch (InvalidOperationException ex)
        {
            LogReconcileFailed(_logger, workspace.AgentId, ex.Message, ex);
            return $"Refused: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' reconciled memory index: +{Added} ~{Updated} -{Removed}, {Total} total, elapsed={ElapsedMs:F2}ms.")]
    private static partial void LogReconciled(ILogger logger, string agentId, int added, int updated, int removed, int total, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "index_memory failed for agent '{AgentId}': {Message}")]
    private static partial void LogReconcileFailed(ILogger logger, string agentId, string message, Exception ex);
}
