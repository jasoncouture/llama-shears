using System.Collections.Immutable;
using System.ComponentModel;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class ListFilesTool
{
    private const int DefaultMaxEntries = 200;
    private const int HardMaxEntries = 1000;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<ListFilesTool> _logger;

    public ListFilesTool(
        IAgentWorkspaceLocator workspace,
        IFileProtectionPolicy protection,
        ILogger<ListFilesTool> logger)
    {
        _workspace = workspace;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_list")]
    [Description("Lists files and directories under the given path on the host filesystem. Returns a JSON object with the resolved path, the recursion flag, an array of entries (each carries name, isDirectory, and sizeBytes for files), the entry count, a truncation flag, and the cap applied. Entries are ordered: directories first, then files, both alphabetically.")]
    public async Task<FileListResult> ListFiles(
        [Description("Path to list. Relative paths resolve against the agent's workspace; absolute paths are honored as-is. Empty (default) lists the agent's workspace root.")] string path = "",
        [Description("If true, recurse into subdirectories. Defaults to false.")] bool recursive = false,
        [Description("Maximum number of entries to return. Defaults to 200; hard-capped at 1000.")] int maxEntries = DefaultMaxEntries,
        CancellationToken cancellationToken = default)
    {
        var cap = Math.Clamp(maxEntries, 1, HardMaxEntries);
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
            return Failure(displayPath, recursive, cap, $"Refused: '{displayPath}' is a file. Use file_read instead.");
        }
        if (!Directory.Exists(resolved))
        {
            return Failure(displayPath, recursive, cap, $"Directory not found: {displayPath}");
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
            return Failure(displayPath, recursive, cap, $"List failed: {ex.Message}");
        }
    }

    private static FileListResult Failure(string path, bool recursive, int cap, string error)
        => new(
            Path: path,
            Recursive: recursive,
            Entries: [],
            EntryCount: 0,
            Truncated: false,
            Cap: cap,
            Error: error);

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' listed {Entries} entries under '{Path}' (truncated={Truncated}).")]
    private partial void LogList(string? agentId, string path, int entries, bool truncated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "List failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogListFailed(string? agentId, string path, string message, Exception ex);
}
