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
    [Description("Stores a memory file in the agent's workspace under memory/YYYY-MM-DD/<unix-seconds>.md. Returns a JSON object with the stored flag and the workspace-relative path of the new memory file. The file is the source of truth and is also embedded into the agent's vector index so memory_search can find it. Indexing failures do not fail the write — the next memory_index will catch up. Convention: lead the file with a single-line summary (typically a markdown H1 like '# Short title — what this memory says'). When this memory matches a future turn, only that first line is auto-injected into context; the full body is fetched on demand via file_read. A weak first line means a weak summary, so the rest of the memory may never get loaded.")]
    public async Task<MemoryStoreResult> StoreMemory(
        [Description("Memory content (markdown). Stored verbatim. The first line is surfaced as the memory's summary in injected context — make it a meaningful one-line description.")] string content,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(workspace.AgentId))
        {
            return Failure("Refused: store_memory requires an authenticated agent on the request.");
        }
        if (string.IsNullOrEmpty(content))
        {
            return Failure("Refused: content is required.");
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
            return Failure($"Refused: {ex.Message}");
        }
    }

    private static MemoryStoreResult Failure(string error)
        => new(
            Stored: false,
            RelativePath: null,
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' stored memory '{Path}'.")]
    private partial void LogStored(string agentId, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "store_memory failed for agent '{AgentId}': {Message}")]
    private partial void LogStoreFailed(string agentId, string message, Exception ex);
}
