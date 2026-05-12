using System.ComponentModel;
using System.Text;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class ReadFileTool
{
    private const long MaxFileBytes = 512 * 1024;

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
    [Description("Reads a file from the host filesystem starting at startLine. Returns up to the shared response budget (~8 KiB / 100 lines); the tail of any longer file is truncated with a marker that reports the line range returned and the next startLine to resume from. Refuses files larger than 512 KiB outright.")]
    public async Task<string> ReadFile(
        [Description("Path to read. Relative paths are resolved against the agent's workspace; absolute paths are honored as-is, anywhere on disk the host can reach.")] string path,
        [Description("First line to return, 1-indexed. Defaults to 1 (start of file).")] int startLine = 1,
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

        var fileInfo = new FileInfo(resolved);
        if (fileInfo.Length > MaxFileBytes)
        {
            LogTooLarge(workspace.AgentId, resolved, fileInfo.Length);
            return $"File is too large: {fileInfo.Length} bytes; the hard cap is {MaxFileBytes} bytes.";
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Read);
        if (protection is not null)
        {
            return ProtectionRefusal.Format(path, ProtectionMode.Read, protection);
        }

        try
        {
            var result = await ReadRangeAsync(resolved, startLine, cancellationToken);
            LogRead(workspace.AgentId, resolved, result.Content.Length, result.Truncated);
            return FormatResponse(result, startLine);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogReadFailed(workspace.AgentId, resolved, ex.Message, ex);
            return $"Read failed: {ex.Message}";
        }
    }

    private static string FormatResponse(ReadResult result, int requestedStartLine)
    {
        if (result.LinesReturned == 0)
        {
            return result.Truncated
                ? $"[empty range; response budget reached before any line at startLine={requestedStartLine} fit. Re-call with a higher startLine to continue reading.]"
                : $"[empty range; file has fewer than {requestedStartLine} lines.]";
        }

        var firstLine = result.FirstLine;
        var lastLine = result.LastLine;
        if (!result.Truncated)
        {
            return $"{result.Content}\n[lines {firstLine}-{lastLine}; end of file.]";
        }
        var nextStart = lastLine + 1;
        return $"{result.Content}\n[lines {firstLine}-{lastLine}; truncated, response budget reached. Re-call with startLine={nextStart} to continue reading.]";
    }

    private static async Task<ReadResult> ReadRangeAsync(
        string fullPath,
        int startLine,
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
        var firstLine = 0;
        var lastLine = 0;

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } current)
        {
            line++;
            if (line < startLine)
            {
                continue;
            }
            if (!ResponseBudget.CanAppendResponse(bytes, collected, current))
            {
                return new ReadResult(builder.ToString(), Truncated: true, firstLine, lastLine, collected);
            }

            if (collected == 0)
            {
                firstLine = line;
            }
            else
            {
                builder.Append('\n');
            }
            builder.Append(current);
            lastLine = line;
            collected++;
            bytes += current.Length + 1;
        }

        return new ReadResult(builder.ToString(), Truncated: false, firstLine, lastLine, collected);
    }

    private readonly record struct ReadResult(
        string Content,
        bool Truncated,
        int FirstLine,
        int LastLine,
        int LinesReturned);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' read {Bytes} bytes from '{Path}' (truncated={Truncated}).")]
    private partial void LogRead(string? agentId, string path, int bytes, bool truncated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Read failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogReadFailed(string? agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' refused to read oversized file '{Path}' ({Bytes} bytes).")]
    private partial void LogTooLarge(string? agentId, string path, long bytes);
}
