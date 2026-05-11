using System.ComponentModel;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Core.Abstractions.Memory;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Memory;

[McpServerToolType]
public sealed partial class StoreMemoryTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IMemoryStore _store;
    private readonly ILogger<StoreMemoryTool> _logger;

    public StoreMemoryTool(IAgentWorkspaceLocator workspace, IMemoryStore store, ILogger<StoreMemoryTool> logger)
    {
        _workspace = workspace;
        _store = store;
        _logger = logger;
    }

    [McpServerTool(Name = "memory_store")]
    [Description("Stores a memory file in the agent's workspace under memory/YYYY-MM-DD/<unix-seconds>.md. The file is the source of truth; it is also embedded into the agent's vector index so memory_search can find it. Indexing failures do not fail the write — the next memory_index will catch up. Convention: lead the file with a single-line summary (typically a markdown H1 like '# Short title — what this memory says'). When this memory matches a future turn, only that first line is auto-injected into context; the full body is fetched on demand via file_read. A weak first line means a weak summary, so the rest of the memory may never get loaded.")]
    public async Task<string> StoreMemory(
        [Description("Memory content (markdown). Stored verbatim. The first line is surfaced as the memory's summary in injected context — make it a meaningful one-line description.")] string content,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return "Refused: store_memory requires an authenticated agent on the request.";
        }
        if (string.IsNullOrEmpty(content))
        {
            return "Refused: content is required.";
        }

        try
        {
            var memoryRef = await _store.StoreAsync(workspace.AgentId, content, cancellationToken);
            LogStored(_logger, workspace.AgentId, memoryRef.RelativePath);
            return $"Stored '{memoryRef.RelativePath}'.";
        }
        catch (InvalidOperationException ex)
        {
            LogStoreFailed(_logger, workspace.AgentId, ex.Message, ex);
            return $"Refused: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' stored memory '{Path}'.")]
    private static partial void LogStored(ILogger logger, string agentId, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "store_memory failed for agent '{AgentId}': {Message}")]
    private static partial void LogStoreFailed(ILogger logger, string agentId, string message, Exception ex);
}
