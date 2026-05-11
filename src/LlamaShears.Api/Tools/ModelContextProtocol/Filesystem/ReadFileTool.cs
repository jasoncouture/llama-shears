using System.ComponentModel;
using System.Text;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class ReadFileTool
{
    private const int ByteCap = 64 * 1024;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<ReadFileTool> _logger;

    public ReadFileTool(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<ReadFileTool> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_read")]
    [Description("Reads a file from the host filesystem. Returns at most 64 KiB from the requested line range and appends a truncation marker if the file content exceeded the cap.")]
    public async Task<string> ReadFile(
        [Description("Path to read. Relative paths are resolved against the agent's workspace; absolute paths are honored as-is, anywhere on disk the host can reach.")] string path,
        [Description("First line to return, 1-indexed. Defaults to 1 (start of file).")] int startLine = 1,
        [Description("Number of lines to return. 0 (default) means read to end of file, subject to the byte cap.")] int lineCount = 0,
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

        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolved = Path.GetFullPath(Path.IsPathRooted(path)
            ? path
            : Path.Combine(workspace.Root, path));

        if (Directory.Exists(resolved))
        {
            return $"Refused: '{path}' is a directory, not a file.";
        }
        if (!File.Exists(resolved))
        {
            return $"File not found: {path}";
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Read);
        if (protection is not null)
        {
            return ProtectionRefusal.Format(path, ProtectionMode.Read, protection);
        }

        try
        {
            var (content, truncated) = await ReadRangeAsync(resolved, startLine, lineCount, ByteCap, cancellationToken);
            LogRead(workspace.AgentId, resolved, content.Length, truncated);
            if (truncated)
            {
                content = $"{content}\n[... truncated; exceeded {ByteCap}-byte cap. Re-call with a tighter line range to see more.]";
            }
            return content;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogReadFailed(workspace.AgentId, resolved, ex.Message, ex);
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' read {Bytes} bytes from '{Path}' (truncated={Truncated}).")]
    private partial void LogRead(string? agentId, string path, int bytes, bool truncated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Read failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogReadFailed(string? agentId, string path, string message, Exception ex);
}
