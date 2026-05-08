using System.ComponentModel;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class MoveFileTool
{
    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<MoveFileTool> _logger;

    public MoveFileTool(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<MoveFileTool> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_move")]
    [Description("Moves a file from source to target inside the agent's workspace. Source needs read and write permissions; target needs write permissions. By default, refuses if the target already exists; pass force=true to overwrite. Refused if the source is missing or either path is inside the protected 'system/' subfolder or matches the workspace file-protection policy. Parent directories are created if missing.")]
    public async Task<string> MoveFile(
        [Description("Source path. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string source,
        [Description("Target path. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string target,
        [Description("If true, overwrite an existing target file. Defaults to false (error if the target exists).")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);

        var sourceResolution = WorkspacePathResolver.ResolveForWrite(workspace, source);
        if (!sourceResolution.IsSuccess)
        {
            return sourceResolution.Error;
        }
        var targetResolution = WorkspacePathResolver.ResolveForWrite(workspace, target);
        if (!targetResolution.IsSuccess)
        {
            return targetResolution.Error;
        }

        if (Directory.Exists(sourceResolution.FullPath))
        {
            return $"Refused: '{source}' is a directory, not a file.";
        }
        if (!File.Exists(sourceResolution.FullPath))
        {
            return $"Source not found: {source}";
        }

        var sourceFullPath = _pathExpander.ExpandPath(source, workspace.Root);
        var sourceRead = _protection.Match(workspace.Root, sourceFullPath, FileType.File, ProtectionMode.Read);
        if (sourceRead is not null)
        {
            return ProtectionRefusal.Format(source, ProtectionMode.Read, sourceRead);
        }
        var sourceWrite = _protection.Match(workspace.Root, sourceFullPath, FileType.File, ProtectionMode.Write);
        if (sourceWrite is not null)
        {
            return ProtectionRefusal.Format(source, ProtectionMode.Write, sourceWrite);
        }

        var targetFullPath = _pathExpander.ExpandPath(target, workspace.Root);
        var targetWrite = _protection.Match(workspace.Root, targetFullPath, FileType.File, ProtectionMode.Write);
        if (targetWrite is not null)
        {
            return ProtectionRefusal.Format(target, ProtectionMode.Write, targetWrite);
        }

        if (Directory.Exists(targetResolution.FullPath))
        {
            return $"Refused: target '{target}' is a directory.";
        }
        if (File.Exists(targetResolution.FullPath) && !force)
        {
            return $"Refused: target '{target}' already exists. Pass force=true to overwrite it.";
        }

        try
        {
            var parent = Path.GetDirectoryName(targetResolution.FullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            File.Move(sourceResolution.FullPath, targetResolution.FullPath, force);
            LogMove(workspace.AgentId, sourceResolution.FullPath, targetResolution.FullPath, force);
            return $"Moved '{source}' to '{target}'.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogMoveFailed(workspace.AgentId, sourceResolution.FullPath, targetResolution.FullPath, ex.Message, ex);
            return $"Move failed: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' moved '{Source}' to '{Target}' (force={Force}).")]
    private partial void LogMove(string? agentId, string source, string target, bool force);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Move failed for agent '{AgentId}' from '{Source}' to '{Target}': {Message}")]
    private partial void LogMoveFailed(string? agentId, string source, string target, string message, Exception ex);
}
