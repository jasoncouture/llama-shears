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
    [Description("Appends content to a file inside the agent's workspace. Returns a JSON object with the path, an appended flag, and bytesAppended. Creates the file (and any missing parent directories) if it does not exist. Writes into the protected 'system/' subfolder, or any path matched by the workspace file-protection policy, are refused. On failure the error field is populated and appended=false.")]
    public async Task<FileAppendResult> AppendFile(
        [Description("Path to append to. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("Content to append. Hard-capped at 1 MiB per call.")] string content,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return Failure(path, resolution.Error);
        }

        content ??= string.Empty;
        var byteCount = Encoding.UTF8.GetByteCount(content);
        if (byteCount > MaxContentBytes)
        {
            return Failure(path, $"Refused: content is {byteCount} bytes; the per-call append cap is {MaxContentBytes} bytes.");
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return Failure(path, $"Refused: '{path}' is an existing directory.");
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Write);
        if (protection is not null)
        {
            return Failure(path, ProtectionRefusal.Format(path, ProtectionMode.Write, protection));
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
            return new FileAppendResult(
                Path: path,
                Appended: true,
                BytesAppended: byteCount);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogAppendFailed(workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return Failure(path, $"Append failed: {ex.Message}");
        }
    }

    private static FileAppendResult Failure(string path, string error)
        => new(
            Path: path,
            Appended: false,
            BytesAppended: 0,
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' appended {Bytes} bytes to '{Path}'.")]
    private partial void LogAppend(string? agentId, string path, int bytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Append failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogAppendFailed(string? agentId, string path, string message, Exception ex);
}
