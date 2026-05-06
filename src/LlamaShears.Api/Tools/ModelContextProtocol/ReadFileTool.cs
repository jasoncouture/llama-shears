using System.ComponentModel;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using LlamaShears.Core.Abstractions.Agent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol;

[McpServerToolType]
public sealed partial class ReadFileTool
{
    private const int DefaultByteCap = 64 * 1024;
    private const int MaxByteCap = 256 * 1024;

    private readonly IHttpContextAccessor _http;
    private readonly IAgentConfigProvider _configs;
    private readonly ILogger<ReadFileTool> _logger;

    public ReadFileTool(
        IHttpContextAccessor http,
        IAgentConfigProvider configs,
        ILogger<ReadFileTool> logger)
    {
        _http = http;
        _configs = configs;
        _logger = logger;
    }

    [McpServerTool(Name = "read_file")]
    [Description("Reads a file from the calling agent's workspace. Returns at most byte_cap bytes from the requested line range and appends a truncation marker if the file content exceeded the cap. The path must resolve inside the agent's workspace; absolute paths outside the workspace are refused.")]
    public async Task<string> ReadFile(
        [Description("Path to read. Relative paths are resolved against the agent's workspace; absolute paths must still resolve inside it.")] string path,
        [Description("First line to return, 1-indexed. Defaults to 1 (start of file).")] int startLine = 1,
        [Description("Number of lines to return. 0 (default) means read to end of file, subject to the byte cap.")] int lineCount = 0,
        [Description("Maximum bytes of content to return. Defaults to 65536 (64 KiB). Hard-capped at 262144 (256 KiB).")] int byteCap = DefaultByteCap,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "path is required.";
        }
        if (startLine < 1)
        {
            startLine = 1;
        }
        if (lineCount < 0)
        {
            lineCount = 0;
        }
        var cap = Math.Clamp(byteCap, 1, MaxByteCap);

        var agentId = _http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(agentId))
        {
            return "Authentication required to read files.";
        }

        var config = await _configs.GetConfigAsync(agentId, cancellationToken).ConfigureAwait(false);
        if (config is null || string.IsNullOrEmpty(config.WorkspacePath))
        {
            return $"Agent '{agentId}' has no configured workspace.";
        }

        var workspace = Path.GetFullPath(config.WorkspacePath);
        var workspacePrefix = workspace.EndsWith(Path.DirectorySeparatorChar)
            ? workspace
            : $"{workspace}{Path.DirectorySeparatorChar}";
        var resolved = Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(workspace, path));

        if (!resolved.StartsWith(workspacePrefix, StringComparison.Ordinal))
        {
            LogPathEscape(_logger, agentId, path, resolved);
            return "Refused: path resolves outside the agent's workspace.";
        }

        if (Directory.Exists(resolved))
        {
            return $"Refused: '{path}' is a directory, not a file.";
        }
        if (!File.Exists(resolved))
        {
            return $"File not found: {path}";
        }

        try
        {
            var (content, truncated) = await ReadRangeAsync(resolved, startLine, lineCount, cap, cancellationToken).ConfigureAwait(false);
            if (truncated)
            {
                content = string.Concat(
                    content,
                    "\n",
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "[... truncated; exceeded {0}-byte cap. Re-call with a tighter line range or a higher byte_cap (max {1}) to see more.]",
                        cap,
                        MaxByteCap));
            }
            return content;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogReadFailed(_logger, agentId, path, ex.Message, ex);
            return $"Read failed: {ex.Message}";
        }
    }

    private static async Task<(string Content, bool Truncated)> ReadRangeAsync(
        string fullPath,
        int startLine,
        int lineCount,
        int byteCap,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var builder = new StringBuilder();
        var line = 0;
        var collected = 0;
        var bytes = 0;

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } current)
        {
            line++;
            if (line < startLine)
            {
                continue;
            }
            if (lineCount > 0 && collected >= lineCount)
            {
                break;
            }

            // +1 for the newline rejoiner. Counted before append so we
            // can stop cleanly the moment the next line would breach
            // the cap.
            var lineBytes = Encoding.UTF8.GetByteCount(current) + 1;
            if (bytes + lineBytes > byteCap)
            {
                return (builder.ToString(), Truncated: true);
            }

            if (collected > 0)
            {
                builder.Append('\n');
            }
            builder.Append(current);
            collected++;
            bytes += lineBytes;
        }

        return (builder.ToString(), Truncated: false);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing read for agent '{AgentId}': path '{Path}' resolved to '{Resolved}' outside workspace.")]
    private static partial void LogPathEscape(ILogger logger, string agentId, string path, string resolved);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Read failed for agent '{AgentId}' path '{Path}': {Message}")]
    private static partial void LogReadFailed(ILogger logger, string agentId, string path, string message, Exception ex);
}
