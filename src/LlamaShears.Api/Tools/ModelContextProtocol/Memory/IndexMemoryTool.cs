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
    [Description("Forces a full reconcile of the agent's memory index against the filesystem. Returns a JSON object with the reconciled flag, added/updated/removed/total counts, and elapsedMilliseconds. Pass force=true to re-embed every file regardless of whether its content has changed — use this if the embedding model or its prompt convention has changed and old vectors need rebuilding.")]
    public async Task<MemoryIndexResult> IndexMemory(
        [Description("If true, re-embed every file even when its content hash already matches the indexed hash. Defaults to false.")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return Failure("Refused: memory_index requires an authenticated agent on the request.");
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
            return Failure($"Refused: {ex.Message}");
        }
    }

    private static MemoryIndexResult Failure(string error)
        => new(
            Reconciled: false,
            Added: 0,
            Updated: 0,
            Removed: 0,
            Total: 0,
            ElapsedMilliseconds: 0,
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' reconciled memory index: +{Added} ~{Updated} -{Removed}, {Total} total, elapsed={ElapsedMs:F2}ms.")]
    private partial void LogReconciled(string agentId, int added, int updated, int removed, int total, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "index_memory failed for agent '{AgentId}': {Message}")]
    private partial void LogReconcileFailed(string agentId, string message, Exception ex);
}
