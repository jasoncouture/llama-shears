using System.ComponentModel;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class DeleteFileTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<DeleteFileTool> _logger;

    public DeleteFileTool(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<DeleteFileTool> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_delete")]
    [Description("Deletes a file or directory inside the agent's workspace. Returns a JSON object with the path, a deleted flag, and a wasDirectory flag. Directories require recursive=true. Deletes inside the protected 'system/' subfolder, or any path matched by the workspace file-protection policy, are refused. On failure the error field is populated and deleted=false.")]
    public async Task<FileDeleteResult> DeleteFile(
        [Description("Path to delete. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("If true, allow deleting a non-empty directory recursively. Defaults to false.")] bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return Failure(path, wasDirectory: false, resolution.Error);
        }

        if (string.Equals(resolution.FullPath, workspace.Root, StringComparison.Ordinal))
        {
            return Failure(path, wasDirectory: true, "Refused: deleting the workspace root is not permitted.");
        }

        var isDir = Directory.Exists(resolution.FullPath);
        var isFile = File.Exists(resolution.FullPath);
        if (!isDir && !isFile)
        {
            return Failure(path, wasDirectory: false, $"Path not found: {path}");
        }

        var actualType = isDir ? FileType.Directory : FileType.File;
        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, actualType, ProtectionMode.Delete);
        if (protection is not null)
        {
            return Failure(path, wasDirectory: isDir, ProtectionRefusal.Format(path, ProtectionMode.Delete, protection));
        }

        try
        {
            if (isDir)
            {
                Directory.Delete(resolution.FullPath, recursive);
                LogDelete(workspace.AgentId, resolution.FullPath, isDirectory: true);
                return new FileDeleteResult(Path: path, Deleted: true, WasDirectory: true);
            }
            File.Delete(resolution.FullPath);
            LogDelete(workspace.AgentId, resolution.FullPath, isDirectory: false);
            return new FileDeleteResult(Path: path, Deleted: true, WasDirectory: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogDeleteFailed(workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return Failure(path, wasDirectory: isDir, $"Delete failed: {ex.Message}");
        }
    }

    private static FileDeleteResult Failure(string path, bool wasDirectory, string error)
        => new(
            Path: path,
            Deleted: false,
            WasDirectory: wasDirectory,
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' deleted '{Path}' (directory={IsDirectory}).")]
    private partial void LogDelete(string? agentId, string path, bool isDirectory);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Delete failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogDeleteFailed(string? agentId, string path, string message, Exception ex);
}
