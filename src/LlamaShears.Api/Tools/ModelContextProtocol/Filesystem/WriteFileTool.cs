using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class WriteFileTool
{
    private const int MaxContentBytes = 1024 * 1024;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<WriteFileTool> _logger;

    public WriteFileTool(IAgentWorkspaceLocator workspace, ILogger<WriteFileTool> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "file_write")]
    [Description("Writes the complete file content to a path within the agent's workspace. By default, refuses if the file already exists; pass overwrite=true to replace it. Writes into the workspace's protected 'system/' subfolder are always refused. Parent directories are created if missing.")]
    public async Task<string> WriteFile(
        [Description("Path to write. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("Complete file contents to write. Hard-capped at 1 MiB.")] string content,
        [Description("If true, replace an existing file. Defaults to false (error if the file exists).")] bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return resolution.Error;
        }

        content ??= string.Empty;
        var byteCount = Encoding.UTF8.GetByteCount(content);
        if (byteCount > MaxContentBytes)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Refused: content is {0} bytes; the per-write cap is {1} bytes.",
                byteCount,
                MaxContentBytes);
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return $"Refused: '{path}' is an existing directory.";
        }
        if (File.Exists(resolution.FullPath) && !overwrite)
        {
            return $"Refused: '{path}' already exists. Pass overwrite=true to replace it.";
        }

        try
        {
            var parent = Path.GetDirectoryName(resolution.FullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            await File.WriteAllTextAsync(resolution.FullPath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            LogWrite(_logger, workspace.AgentId, resolution.FullPath, byteCount, overwrite);
            return string.Format(
                CultureInfo.InvariantCulture,
                "Wrote {0} bytes to '{1}'.",
                byteCount,
                path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogWriteFailed(_logger, workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return $"Write failed: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' wrote {Bytes} bytes to '{Path}' (overwrite={Overwrite}).")]
    private static partial void LogWrite(ILogger logger, string? agentId, string path, int bytes, bool overwrite);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Write failed for agent '{AgentId}' path '{Path}': {Message}")]
    private static partial void LogWriteFailed(ILogger logger, string? agentId, string path, string message, Exception ex);
}
