using System.ComponentModel;
using System.Text;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class AppendFileTool
{
    private const int MaxContentBytes = 1024 * 1024;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<AppendFileTool> _logger;

    public AppendFileTool(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<AppendFileTool> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_append")]
    [Description("Appends content to a file inside the agent's workspace. Creates the file (and any missing parent directories) if it does not exist. Writes into the protected 'system/' subfolder, or any path matched by the workspace file-protection policy (e.g. root '.gitignore'), are refused.")]
    public async Task<string> AppendFile(
        [Description("Path to append to. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("Content to append. Hard-capped at 1 MiB per call.")] string content,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return resolution.Error;
        }

        content ??= string.Empty;
        var byteCount = Encoding.UTF8.GetByteCount(content);
        if (byteCount > MaxContentBytes)
        {
            return $"Refused: content is {byteCount} bytes; the per-call append cap is {MaxContentBytes} bytes.";
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return $"Refused: '{path}' is an existing directory.";
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Write);
        if (protection is not null)
        {
            return ProtectionRefusal.Format(path, ProtectionMode.Write, protection);
        }

        try
        {
            var parent = Path.GetDirectoryName(resolution.FullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            await File.AppendAllTextAsync(resolution.FullPath, content, Encoding.UTF8, cancellationToken);
            LogAppend(workspace.AgentId, resolution.FullPath, byteCount);
            return $"Appended {byteCount} bytes to '{path}'.";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogAppendFailed(workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return $"Append failed: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' appended {Bytes} bytes to '{Path}'.")]
    private partial void LogAppend(string? agentId, string path, int bytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Append failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogAppendFailed(string? agentId, string path, string message, Exception ex);
}
