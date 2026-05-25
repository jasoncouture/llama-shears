using System.Collections.Immutable;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class FileTools
{
    private const int DefaultListMaxEntries = 200;
    private const int HardMaxListEntries = 1000;
    private const int MaxWriteBytes = 1024 * 1024;
    private const int MaxAppendBytes = 1024 * 1024;
    private const int MaxRegexReplaceBytes = 4 * 1024 * 1024;
    private const int DefaultGrepMaxMatches = 200;
    private const int HardMaxGrepMatches = 1000;
    private const long GrepPerFileByteCap = 8L * 1024 * 1024;
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<FileTools> _logger;

    public FileTools(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<FileTools> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_read", Destructive = false, OpenWorld = false, ReadOnly = true)]
    [Description("Reads a file from the host filesystem starting at startLine. Returns a JSON object with the line range read, the content, the file's createdAt/modifiedAt timestamps (local time), and an endOfFile flag. A single call is capped by the shared response budget; when endOfFile is false, re-call with startLine = endLine + 1 to continue. On failure, the error field is populated and content is empty.")]
    public async Task<FileReadResult> ReadFile(
        [Description("Path to read. Relative paths are resolved against the agent's workspace; absolute paths are honored as-is, anywhere on disk the host can reach.")] string path,
        [Description("First line to return, 1-indexed. Defaults to 1 (start of file).")] int startLine = 1,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ReadFailure(path ?? string.Empty, startLine, "path is required.");
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
            return ReadFailure(path, startLine, $"Refused: '{path}' is a directory, not a file.");
        }
        if (!File.Exists(resolved))
        {
            return ReadFailure(path, startLine, $"File not found: {path}");
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Read);
        if (protection is not null)
        {
            return ReadFailure(path, startLine, ProtectionRefusal.Format(path, ProtectionMode.Read, protection));
        }

        var fileInfo = new FileInfo(resolved);
        var createdAt = new DateTimeOffset(fileInfo.CreationTime);
        var modifiedAt = new DateTimeOffset(fileInfo.LastWriteTime);

        try
        {
            var result = await ReadRangeAsync(resolved, startLine, cancellationToken);
            LogRead(workspace.AgentId, resolved, result.Content.Length, result.Truncated);
            return BuildReadResponse(path, startLine, result, createdAt, modifiedAt);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogReadFailed(workspace.AgentId, resolved, ex.Message, ex);
            return ReadFailure(path, startLine, $"Read failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "file_list", Destructive = false, OpenWorld = false, ReadOnly = true)]
    [Description("Lists files and directories under the given path on the host filesystem. Returns a JSON object with the resolved path, the recursion flag, an array of entries (each carries name, isDirectory, and sizeBytes for files), the entry count, a truncation flag, and the cap applied. Entries are ordered: directories first, then files, both alphabetically.")]
    public async Task<FileListResult> ListFiles(
        [Description("Path to list. Relative paths resolve against the agent's workspace; absolute paths are honored as-is. Empty (default) lists the agent's workspace root.")] string path = "",
        [Description("If true, recurse into subdirectories. Defaults to false.")] bool recursive = false,
        [Description("Maximum number of entries to return. Defaults to 200; hard-capped at 1000.")] int maxEntries = DefaultListMaxEntries,
        CancellationToken cancellationToken = default)
    {
        var cap = Math.Clamp(maxEntries, 1, HardMaxListEntries);
        var workspace = await _workspace.GetAsync(cancellationToken);

        string resolved;
        string displayPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            resolved = workspace.Root;
            displayPath = "(workspace root)";
        }
        else
        {
            resolved = Path.GetFullPath(Path.IsPathRooted(path)
                ? path
                : Path.Combine(workspace.Root, path));
            displayPath = path;
        }

        if (File.Exists(resolved))
        {
            return ListFailure(displayPath, recursive, cap, $"Refused: '{displayPath}' is a file. Use file_read instead.");
        }
        if (!Directory.Exists(resolved))
        {
            return ListFailure(displayPath, recursive, cap, $"Directory not found: {displayPath}");
        }

        try
        {
            var entries = Collect(resolved, workspace.Root, recursive, cap, _protection, out var truncated);
            LogList(workspace.AgentId, resolved, entries.Length, truncated);
            return new FileListResult(
                Path: displayPath,
                Recursive: recursive,
                Entries: entries,
                EntryCount: entries.Length,
                Truncated: truncated,
                Cap: cap);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogListFailed(workspace.AgentId, resolved, ex.Message, ex);
            return ListFailure(displayPath, recursive, cap, $"List failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "file_write", Idempotent = true, OpenWorld = false)]
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
            return WriteFailure(path, resolution.Error);
        }

        content ??= string.Empty;
        var byteCount = Encoding.UTF8.GetByteCount(content);
        if (byteCount > MaxWriteBytes)
        {
            return WriteFailure(path, $"Refused: content is {byteCount} bytes; the per-write cap is {MaxWriteBytes} bytes.");
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return WriteFailure(path, $"Refused: '{path}' is an existing directory.");
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Write);
        if (protection is not null)
        {
            return WriteFailure(path, ProtectionRefusal.Format(path, ProtectionMode.Write, protection));
        }

        var existed = File.Exists(resolution.FullPath);
        if (existed && !overwrite)
        {
            return WriteFailure(path, $"Refused: '{path}' already exists. Pass overwrite=true to replace it.");
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
            return WriteFailure(path, $"Write failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "file_append", OpenWorld = false)]
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
            return AppendFailure(path, resolution.Error);
        }

        content ??= string.Empty;
        var byteCount = Encoding.UTF8.GetByteCount(content);
        if (byteCount > MaxAppendBytes)
        {
            return AppendFailure(path, $"Refused: content is {byteCount} bytes; the per-call append cap is {MaxAppendBytes} bytes.");
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return AppendFailure(path, $"Refused: '{path}' is an existing directory.");
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Write);
        if (protection is not null)
        {
            return AppendFailure(path, ProtectionRefusal.Format(path, ProtectionMode.Write, protection));
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
            return AppendFailure(path, $"Append failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "file_delete", Idempotent = true, OpenWorld = false)]
    [Description("Deletes a file or directory inside the agent's workspace. Returns a JSON object with the path, a deleted flag, and a wasDirectory flag. Directories require recursive=true. Deletes inside the protected 'system/' subfolder, or any path matched by the workspace file-protection policy, are refused. On failure the error field is populated and deleted=false.")]
    public async Task<FileDeleteResult> DeleteFile(
        [Description("Path to delete. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description("If true, allow deleting a non-empty directory recursively. Defaults to false.")] bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return DeleteFailure(path, wasDirectory: false, resolution.Error);
        }

        if (string.Equals(resolution.FullPath, workspace.Root, StringComparison.Ordinal))
        {
            return DeleteFailure(path, wasDirectory: true, "Refused: deleting the workspace root is not permitted.");
        }

        var isDir = Directory.Exists(resolution.FullPath);
        var isFile = File.Exists(resolution.FullPath);
        if (!isDir && !isFile)
        {
            return DeleteFailure(path, wasDirectory: false, $"Path not found: {path}");
        }

        var actualType = isDir ? FileType.Directory : FileType.File;
        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, actualType, ProtectionMode.Delete);
        if (protection is not null)
        {
            return DeleteFailure(path, wasDirectory: isDir, ProtectionRefusal.Format(path, ProtectionMode.Delete, protection));
        }

        try
        {
            if (isDir)
            {
                Directory.Delete(resolution.FullPath, recursive);
                LogDelete(workspace.AgentId, resolution.FullPath, isDirectory: true);
                return new FileDeleteResult(Path: path, Deleted: true, WasDirectory: true);
            }
            File.Delete(resolution.FullPath);
            LogDelete(workspace.AgentId, resolution.FullPath, isDirectory: false);
            return new FileDeleteResult(Path: path, Deleted: true, WasDirectory: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogDeleteFailed(workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return DeleteFailure(path, wasDirectory: isDir, $"Delete failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "file_move", OpenWorld = false)]
    [Description("Moves a file from source to target inside the agent's workspace. Returns a JSON object with source, target, moved flag, and overwritten flag. Source needs read+write permissions; target needs write. By default refuses if the target already exists; pass force=true to overwrite. Refused if source is missing or either path is in the protected 'system/' subfolder or matches the workspace file-protection policy. Parent directories are created if missing. On failure the error field is populated and moved=false.")]
    public async Task<FileMoveResult> MoveFile(
        [Description("Source path. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string source,
        [Description("Target path. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string target,
        [Description("If true, overwrite an existing target file. Defaults to false (error if the target exists).")] bool force = false,
        CancellationToken cancellationToken = default)
    {
        var workspace = await _workspace.GetAsync(cancellationToken);

        var sourceResolution = WorkspacePathResolver.ResolveForWrite(workspace, source);
        if (!sourceResolution.IsSuccess)
        {
            return MoveFailure(source, target, sourceResolution.Error);
        }
        var targetResolution = WorkspacePathResolver.ResolveForWrite(workspace, target);
        if (!targetResolution.IsSuccess)
        {
            return MoveFailure(source, target, targetResolution.Error);
        }

        if (Directory.Exists(sourceResolution.FullPath))
        {
            return MoveFailure(source, target, $"Refused: '{source}' is a directory, not a file.");
        }
        if (!File.Exists(sourceResolution.FullPath))
        {
            return MoveFailure(source, target, $"Source not found: {source}");
        }

        var sourceFullPath = _pathExpander.ExpandPath(source, workspace.Root);
        var sourceRead = _protection.Match(workspace.Root, sourceFullPath, FileType.File, ProtectionMode.Read);
        if (sourceRead is not null)
        {
            return MoveFailure(source, target, ProtectionRefusal.Format(source, ProtectionMode.Read, sourceRead));
        }
        var sourceWrite = _protection.Match(workspace.Root, sourceFullPath, FileType.File, ProtectionMode.Write);
        if (sourceWrite is not null)
        {
            return MoveFailure(source, target, ProtectionRefusal.Format(source, ProtectionMode.Write, sourceWrite));
        }

        var targetFullPath = _pathExpander.ExpandPath(target, workspace.Root);
        var targetWrite = _protection.Match(workspace.Root, targetFullPath, FileType.File, ProtectionMode.Write);
        if (targetWrite is not null)
        {
            return MoveFailure(source, target, ProtectionRefusal.Format(target, ProtectionMode.Write, targetWrite));
        }

        if (Directory.Exists(targetResolution.FullPath))
        {
            return MoveFailure(source, target, $"Refused: target '{target}' is a directory.");
        }
        var targetExisted = File.Exists(targetResolution.FullPath);
        if (targetExisted && !force)
        {
            return MoveFailure(source, target, $"Refused: target '{target}' already exists. Pass force=true to overwrite it.");
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
            return new FileMoveResult(
                Source: source,
                Target: target,
                Moved: true,
                Overwritten: targetExisted);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogMoveFailed(workspace.AgentId, sourceResolution.FullPath, targetResolution.FullPath, ex.Message, ex);
            return MoveFailure(source, target, $"Move failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "file_regex_replace", OpenWorld = false)]
    [Description("Edits a file in place by applying a .NET regex replacement. Returns a JSON object with the path, an edited flag, and replacement count. edited is true only when at least one match was replaced and the file was rewritten; a zero-match call returns edited=false with no error. Files in the protected 'system/' subfolder or matching the workspace file-protection policy cannot be edited. Hard-capped to files <= 4 MiB.")]
    public async Task<FileRegexReplaceResult> RegexReplaceFile(
        [Description("Path to edit. Relative paths resolve against the agent's workspace; absolute paths must still resolve inside the workspace.")] string path,
        [Description(".NET regex pattern to match.")] string pattern,
        [Description("Replacement string. Supports the standard .NET replacement tokens ($1, ${name}, $$, etc.).")] string replacement,
        [Description("If true, match case-insensitively. Defaults to false.")] bool caseInsensitive = false,
        [Description("If true, ^ and $ match line boundaries instead of input boundaries. Defaults to true.")] bool multiline = true,
        [Description("Maximum number of replacements to make. 0 (default) means unlimited.")] int maxReplacements = 0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return RegexReplaceFailure(path, "pattern is required.");
        }
        replacement ??= string.Empty;

        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return RegexReplaceFailure(path, resolution.Error);
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return RegexReplaceFailure(path, $"Refused: '{path}' is a directory.");
        }
        if (!File.Exists(resolution.FullPath))
        {
            return RegexReplaceFailure(path, $"File not found: {path}");
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Write);
        if (protection is not null)
        {
            return RegexReplaceFailure(path, ProtectionRefusal.Format(path, ProtectionMode.Write, protection));
        }

        var info = new FileInfo(resolution.FullPath);
        if (info.Length > MaxRegexReplaceBytes)
        {
            return RegexReplaceFailure(path, $"Refused: file is {info.Length} bytes; the regex-replace cap is {MaxRegexReplaceBytes} bytes.");
        }

        Regex regex;
        try
        {
            var options = RegexOptions.CultureInvariant;
            if (caseInsensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }
            if (multiline)
            {
                options |= RegexOptions.Multiline;
            }
            regex = new Regex(pattern, options, _regexTimeout);
        }
        catch (ArgumentException ex)
        {
            return RegexReplaceFailure(path, $"Invalid regex: {ex.Message}");
        }

        try
        {
            var original = await File.ReadAllTextAsync(resolution.FullPath, Encoding.UTF8, cancellationToken);
            var count = 0;
            var limit = maxReplacements <= 0 ? -1 : maxReplacements;
            string updated;
            try
            {
                updated = regex.Replace(original, match =>
                {
                    count++;
                    return match.Result(replacement);
                }, limit);
            }
            catch (RegexMatchTimeoutException ex)
            {
                return RegexReplaceFailure(path, $"Regex timed out after {ex.MatchTimeout.TotalSeconds:N0}s; tighten the pattern.");
            }

            if (count == 0)
            {
                LogReplace(workspace.AgentId, resolution.FullPath, count);
                return new FileRegexReplaceResult(Path: path, Edited: false, Replacements: 0);
            }

            await File.WriteAllTextAsync(resolution.FullPath, updated, Encoding.UTF8, cancellationToken);
            LogReplace(workspace.AgentId, resolution.FullPath, count);
            return new FileRegexReplaceResult(Path: path, Edited: true, Replacements: count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogReplaceFailed(workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return RegexReplaceFailure(path, $"Replace failed: {ex.Message}");
        }
    }

    [McpServerTool(Name = "file_grep", Destructive = false, OpenWorld = false, ReadOnly = true)]
    [Description("Searches the agent's workspace for a regex across files matching a path glob. Returns a JSON object with the glob, files-scanned and match counts, a truncation flag with the applied cap, and an array of matches (each carries workspace-relative path, 1-based line and column, and the full matched line). On failure the error field is populated and matches is empty.")]
    public async Task<GrepResult> Grep(
        [Description(".NET regex pattern to match against each line.")] string pattern,
        [Description("Path glob (Microsoft.Extensions.FileSystemGlobbing syntax, e.g. '**/*.cs') anchored at the workspace root. Defaults to '**/*'.")] string pathGlob = "**/*",
        [Description("If true, match case-insensitively. Defaults to false.")] bool caseInsensitive = false,
        [Description("Maximum number of matches to return. Defaults to 200; hard-capped at 1000.")] int maxMatches = DefaultGrepMaxMatches,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return GrepFailure(pathGlob, DefaultGrepMaxMatches, "pattern is required.");
        }
        if (string.IsNullOrWhiteSpace(pathGlob))
        {
            pathGlob = "**/*";
        }
        var cap = Math.Clamp(maxMatches, 1, HardMaxGrepMatches);

        Regex regex;
        try
        {
            var options = RegexOptions.CultureInvariant;
            if (caseInsensitive)
            {
                options |= RegexOptions.IgnoreCase;
            }
            regex = new Regex(pattern, options, _regexTimeout);
        }
        catch (ArgumentException ex)
        {
            return GrepFailure(pathGlob, cap, $"Invalid regex: {ex.Message}");
        }

        var workspace = await _workspace.GetAsync(cancellationToken);
        if (!Directory.Exists(workspace.Root))
        {
            return GrepFailure(pathGlob, cap, $"Workspace not found: {workspace.Root}");
        }

        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddInclude(pathGlob);
        var matchResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(workspace.Root)));
        if (!matchResult.HasMatches)
        {
            LogGrep(workspace.AgentId, pathGlob, files: 0, matches: 0, truncated: false);
            return new GrepResult(
                PathGlob: pathGlob,
                FilesScanned: 0,
                MatchCount: 0,
                Truncated: false,
                Cap: cap,
                Matches: []);
        }

        var matches = ImmutableArray.CreateBuilder<GrepMatch>();
        var truncated = false;
        var filesScanned = 0;

        foreach (var hit in matchResult.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (matches.Count >= cap)
            {
                truncated = true;
                break;
            }

            var fullPath = Path.GetFullPath(Path.Combine(workspace.Root, hit.Path));
            if (!File.Exists(fullPath))
            {
                continue;
            }
            if (_protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Read) is not null)
            {
                continue;
            }

            long size;
            try
            {
                size = new FileInfo(fullPath).Length;
            }
            catch (IOException)
            {
                continue;
            }
            if (size > GrepPerFileByteCap)
            {
                continue;
            }

            filesScanned++;
            try
            {
                await ScanFileAsync(fullPath, hit.Path, regex, matches, cap, cancellationToken);
            }
            catch (RegexMatchTimeoutException)
            {
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _ = ex;
            }

            if (matches.Count >= cap)
            {
                truncated = true;
                break;
            }
        }

        LogGrep(workspace.AgentId, pathGlob, filesScanned, matches.Count, truncated);
        return new GrepResult(
            PathGlob: pathGlob,
            FilesScanned: filesScanned,
            MatchCount: matches.Count,
            Truncated: truncated,
            Cap: cap,
            Matches: matches.ToImmutable());
    }

    private static FileReadResult BuildReadResponse(
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
                Content: string.Empty,
                CreatedAt: createdAt,
                ModifiedAt: modifiedAt);
        }

        return new FileReadResult(
            Path: path,
            StartLine: result.FirstLine,
            EndLine: result.LastLine,
            LinesReturned: result.LinesReturned,
            EndOfFile: !result.Truncated,
            Content: result.Content,
            CreatedAt: createdAt,
            ModifiedAt: modifiedAt);
    }

    private static FileReadResult ReadFailure(string path, int startLine, string error)
        => new(
            Path: path,
            StartLine: startLine,
            EndLine: startLine - 1,
            LinesReturned: 0,
            EndOfFile: true,
            Content: string.Empty,
            Error: error);

    private static FileListResult ListFailure(string path, bool recursive, int cap, string error)
        => new(
            Path: path,
            Recursive: recursive,
            Entries: [],
            EntryCount: 0,
            Truncated: false,
            Cap: cap,
            Error: error);

    private static FileWriteResult WriteFailure(string path, string error)
        => new(
            Path: path,
            Written: false,
            BytesWritten: 0,
            Overwritten: false,
            Error: error);

    private static FileAppendResult AppendFailure(string path, string error)
        => new(
            Path: path,
            Appended: false,
            BytesAppended: 0,
            Error: error);

    private static FileDeleteResult DeleteFailure(string path, bool wasDirectory, string error)
        => new(
            Path: path,
            Deleted: false,
            WasDirectory: wasDirectory,
            Error: error);

    private static FileMoveResult MoveFailure(string source, string target, string error)
        => new(
            Source: source,
            Target: target,
            Moved: false,
            Overwritten: false,
            Error: error);

    private static FileRegexReplaceResult RegexReplaceFailure(string path, string error)
        => new(
            Path: path,
            Edited: false,
            Replacements: 0,
            Error: error);

    private static GrepResult GrepFailure(string pathGlob, int cap, string error)
        => new(
            PathGlob: pathGlob,
            FilesScanned: 0,
            MatchCount: 0,
            Truncated: false,
            Cap: cap,
            Matches: [],
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
            FileShare.ReadWrite | FileShare.Delete,
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

    private static ImmutableArray<FileListEntry> Collect(
        string root,
        string workspaceRoot,
        bool recursive,
        int cap,
        IFileProtectionPolicy protection,
        out bool truncated)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var directories = Directory
            .EnumerateDirectories(root, "*", option)
            .Where(p => protection.Match(workspaceRoot, p, FileType.Directory, ProtectionMode.Read) is null)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        var files = Directory
            .EnumerateFiles(root, "*", option)
            .Where(p => protection.Match(workspaceRoot, p, FileType.File, ProtectionMode.Read) is null)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

        var builder = ImmutableArray.CreateBuilder<FileListEntry>();
        truncated = false;

        foreach (var dir in directories)
        {
            if (builder.Count >= cap)
            {
                truncated = true;
                break;
            }
            builder.Add(new FileListEntry(
                Name: Path.GetRelativePath(root, dir),
                IsDirectory: true,
                SizeBytes: null));
        }

        if (!truncated)
        {
            foreach (var file in files)
            {
                if (builder.Count >= cap)
                {
                    truncated = true;
                    break;
                }
                long? size = null;
                try
                {
                    size = new FileInfo(file).Length;
                }
                catch (IOException)
                {
                }
                builder.Add(new FileListEntry(
                    Name: Path.GetRelativePath(root, file),
                    IsDirectory: false,
                    SizeBytes: size));
            }
        }

        return builder.ToImmutable();
    }

    private static async Task ScanFileAsync(
        string fullPath,
        string relativePath,
        Regex regex,
        ImmutableArray<GrepMatch>.Builder matches,
        int cap,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 4096,
            useAsync: true);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineNumber++;
            var match = regex.Match(line);
            while (match.Success)
            {
                if (matches.Count >= cap)
                {
                    return;
                }
                matches.Add(new GrepMatch(
                    Path: relativePath,
                    Line: lineNumber,
                    Column: match.Index + 1,
                    Text: line));
                if (match.Length == 0)
                {
                    break;
                }
                match = match.NextMatch();
            }
        }
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' listed {Entries} entries under '{Path}' (truncated={Truncated}).")]
    private partial void LogList(string? agentId, string path, int entries, bool truncated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "List failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogListFailed(string? agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' wrote {Bytes} bytes to '{Path}' (overwrite={Overwrite}).")]
    private partial void LogWrite(string? agentId, string path, int bytes, bool overwrite);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Write failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogWriteFailed(string? agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' appended {Bytes} bytes to '{Path}'.")]
    private partial void LogAppend(string? agentId, string path, int bytes);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Append failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogAppendFailed(string? agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' deleted '{Path}' (directory={IsDirectory}).")]
    private partial void LogDelete(string? agentId, string path, bool isDirectory);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Delete failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogDeleteFailed(string? agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' moved '{Source}' to '{Target}' (force={Force}).")]
    private partial void LogMove(string? agentId, string source, string target, bool force);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Move failed for agent '{AgentId}' from '{Source}' to '{Target}': {Message}")]
    private partial void LogMoveFailed(string? agentId, string source, string target, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' applied regex replace to '{Path}' ({Count} matches).")]
    private partial void LogReplace(string? agentId, string path, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Regex replace failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogReplaceFailed(string? agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' grep glob '{Glob}' scanned {Files} files, {Matches} matches (truncated={Truncated}).")]
    private partial void LogGrep(string? agentId, string glob, int files, int matches, bool truncated);
}
