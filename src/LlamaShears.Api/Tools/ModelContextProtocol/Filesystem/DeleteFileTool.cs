using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class DeleteFileTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<DeleteFileTool> _logger;

    public DeleteFileTool(IAgentWorkspaceLocator workspace, ILogger<DeleteFileTool> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "file_delete")]
    [Description("Deletes a file or directory inside the agent's workspace. Directories require recursive=true. Deletes inside the protected 'system/' subfolder are refused.")]
    public async Task<string> DeleteFile(
        [Description("Path to delete. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("If true, allow deleting a non-empty directory recursively. Defaults to false.")] bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return resolution.Error;
        }

        if (string.Equals(resolution.FullPath, workspace.Root, StringComparison.Ordinal))
        {
            return "Refused: deleting the workspace root is not permitted.";
        }

        var isDir = Directory.Exists(resolution.FullPath);
        var isFile = File.Exists(resolution.FullPath);
        if (!isDir && !isFile)
        {
            return $"Path not found: {path}";
        }

        try
        {
            if (isDir)
            {
                Directory.Delete(resolution.FullPath, recursive);
                LogDelete(_logger, workspace.AgentId, resolution.FullPath, IsDirectory: true);
                return $"Deleted directory '{path}'.";
            }
            File.Delete(resolution.FullPath);
            LogDelete(_logger, workspace.AgentId, resolution.FullPath, IsDirectory: false);
            return $"Deleted file '{path}'.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogDeleteFailed(_logger, workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return $"Delete failed: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' deleted '{Path}' (directory={IsDirectory}).")]
    private static partial void LogDelete(ILogger logger, string? agentId, string path, bool IsDirectory);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Delete failed for agent '{AgentId}' path '{Path}': {Message}")]
    private static partial void LogDeleteFailed(ILogger logger, string? agentId, string path, string message, Exception ex);
}
