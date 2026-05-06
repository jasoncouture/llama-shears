using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class AppendFileTool
{
    private const int MaxContentBytes = 1024 * 1024;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<AppendFileTool> _logger;

    public AppendFileTool(IAgentWorkspaceLocator workspace, ILogger<AppendFileTool> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "file_append")]
    [Description("Appends content to a file inside the agent's workspace. Creates the file (and any missing parent directories) if it does not exist. Writes into the protected 'system/' subfolder are refused.")]
    public async Task<string> AppendFile(
        [Description("Path to append to. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("Content to append. Hard-capped at 1 MiB per call.")] string content,
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
                "Refused: content is {0} bytes; the per-call append cap is {1} bytes.",
                byteCount,
                MaxContentBytes);
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return $"Refused: '{path}' is an existing directory.";
        }

        try
        {
            var parent = Path.GetDirectoryName(resolution.FullPath);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
            await File.AppendAllTextAsync(resolution.FullPath, content, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            LogAppend(_logger, workspace.AgentId, resolution.FullPath, byteCount);
            return string.Format(
                CultureInfo.InvariantCulture,
                "Appended {0} bytes to '{1}'.",
                byteCount,
                path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogAppendFailed(_logger, workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return $"Append failed: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' appended {Bytes} bytes to '{Path}'.")]
    private static partial void LogAppend(ILogger logger, string? agentId, string path, int bytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Append failed for agent '{AgentId}' path '{Path}': {Message}")]
    private static partial void LogAppendFailed(ILogger logger, string? agentId, string path, string message, Exception ex);
}
