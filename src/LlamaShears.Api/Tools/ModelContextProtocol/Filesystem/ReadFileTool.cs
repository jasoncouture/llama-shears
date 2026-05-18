using System.ComponentModel;
using System.Text;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class ReadFileTool
{
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
    [Description("Reads a file from the host filesystem starting at startLine. Returns a JSON object with the line range read, the content, the file's createdAt/modifiedAt timestamps (local time), an endOfFile flag, and (when more lines remain) a nextStartLine the caller should use to resume. A single call is capped by the shared response budget; re-call with the reported nextStartLine to continue until endOfFile is true. On failure, the error field is populated and content is empty.")]
    public async Task<FileReadResult> ReadFile(
        [Description("Path to read. Relative paths are resolved against the agent's workspace; absolute paths are honored as-is, anywhere on disk the host can reach.")] string path,
        [Description("First line to return, 1-indexed. Defaults to 1 (start of file).")] int startLine = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Failure(path ?? string.Empty, startLine, "path is required.");
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
            return Failure(path, startLine, $"Refused: '{path}' is a directory, not a file.");
        }
        if (!File.Exists(resolved))
        {
            return Failure(path, startLine, $"File not found: {path}");
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Read);
        if (protection is not null)
        {
            return Failure(path, startLine, ProtectionRefusal.Format(path, ProtectionMode.Read, protection));
        }

        var fileInfo = new FileInfo(resolved);
        var createdAt = new DateTimeOffset(fileInfo.CreationTime);
        var modifiedAt = new DateTimeOffset(fileInfo.LastWriteTime);

        try
        {
            var result = await ReadRangeAsync(resolved, startLine, cancellationToken);
            LogRead(workspace.AgentId, resolved, result.Content.Length, result.Truncated);
            return BuildResponse(path, startLine, result, createdAt, modifiedAt);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogReadFailed(workspace.AgentId, resolved, ex.Message, ex);
            return Failure(path, startLine, $"Read failed: {ex.Message}");
        }
    }

    private static FileReadResult BuildResponse(
        string path,
        int requestedStartLine,
        ReadResult result,
        DateTimeOffset createdAt,
        DateTimeOffset modifiedAt)
    {
        if (result.LinesReturned == 0)
        {
            return new FileReadResult(
                Path: path,
                StartLine: requestedStartLine,
                EndLine: requestedStartLine - 1,
                LinesReturned: 0,
                EndOfFile: !result.Truncated,
                NextStartLine: result.Truncated ? requestedStartLine + 1 : null,
                Content: string.Empty,
                CreatedAt: createdAt,
                ModifiedAt: modifiedAt);
        }

        var endOfFile = !result.Truncated;
        return new FileReadResult(
            Path: path,
            StartLine: result.FirstLine,
            EndLine: result.LastLine,
            LinesReturned: result.LinesReturned,
            EndOfFile: endOfFile,
            NextStartLine: endOfFile ? null : result.LastLine + 1,
            Content: result.Content,
            CreatedAt: createdAt,
            ModifiedAt: modifiedAt);
    }

    private static FileReadResult Failure(string path, int startLine, string error)
        => new(
            Path: path,
            StartLine: startLine,
            EndLine: startLine - 1,
            LinesReturned: 0,
            EndOfFile: true,
            NextStartLine: null,
            Content: string.Empty,
            Error: error);

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
}
