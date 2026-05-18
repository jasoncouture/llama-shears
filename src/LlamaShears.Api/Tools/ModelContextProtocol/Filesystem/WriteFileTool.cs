using System.ComponentModel;
using System.Text;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class WriteFileTool
{
    private const int MaxContentBytes = 1024 * 1024;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<WriteFileTool> _logger;

    public WriteFileTool(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<WriteFileTool> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_write")]
    [Description("Writes the complete file content to a path within the agent's workspace. Returns a JSON object with the path, a written flag, bytesWritten, and whether an existing file was overwritten. By default, refuses if the file already exists; pass overwrite=true to replace it. Writes into the workspace's protected 'system/' subfolder, or any path matched by the workspace file-protection policy, are refused. Parent directories are created if missing. On failure the error field is populated and written=false.")]
    public async Task<FileWriteResult> WriteFile(
        [Description("Path to write. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("Complete file contents to write. Hard-capped at 1 MiB.")] string content,
        [Description("If true, replace an existing file. Defaults to false (error if the file exists).")] bool overwrite = false,
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
            return Failure(path, $"Refused: content is {byteCount} bytes; the per-write cap is {MaxContentBytes} bytes.");
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

        var existed = File.Exists(resolution.FullPath);
        if (existed && !overwrite)
        {
            return Failure(path, $"Refused: '{path}' already exists. Pass overwrite=true to replace it.");
        }

        try
        {
            var parent = Path.GetDirectoryName(resolution.FullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            await File.WriteAllTextAsync(resolution.FullPath, content, Encoding.UTF8, cancellationToken);
            LogWrite(workspace.AgentId, resolution.FullPath, byteCount, existed);
            return new FileWriteResult(
                Path: path,
                Written: true,
                BytesWritten: byteCount,
                Overwritten: existed);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogWriteFailed(workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return Failure(path, $"Write failed: {ex.Message}");
        }
    }

    private static FileWriteResult Failure(string path, string error)
        => new(
            Path: path,
            Written: false,
            BytesWritten: 0,
            Overwritten: false,
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' wrote {Bytes} bytes to '{Path}' (overwrite={Overwrite}).")]
    private partial void LogWrite(string? agentId, string path, int bytes, bool overwrite);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Write failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogWriteFailed(string? agentId, string path, string message, Exception ex);
}
