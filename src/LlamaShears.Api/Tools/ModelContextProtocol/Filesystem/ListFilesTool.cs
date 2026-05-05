using System.ComponentModel;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class ListFilesTool
{
    private const int DefaultMaxEntries = 200;
    private const int HardMaxEntries = 1000;

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<ListFilesTool> _logger;

    public ListFilesTool(IAgentWorkspaceLocator workspace, ILogger<ListFilesTool> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "file_list")]
    [Description("Lists files and directories under the given path on the host filesystem. Directories are listed first (with a trailing slash), then files (with byte size). Output is capped; a truncation marker is appended when the listing exceeds the cap.")]
    public async Task<string> ListFiles(
        [Description("Path to list. Relative paths resolve against the agent's workspace; absolute paths are honored as-is. Empty (default) lists the agent's workspace root.")] string path = "",
        [Description("If true, recurse into subdirectories. Defaults to false.")] bool recursive = false,
        [Description("Maximum number of entries to return. Defaults to 200; hard-capped at 1000.")] int maxEntries = DefaultMaxEntries,
        CancellationToken cancellationToken = default)
    {
        var cap = Math.Clamp(maxEntries, 1, HardMaxEntries);
        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);

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
            return $"Refused: '{displayPath}' is a file. Use read_file instead.";
        }
        if (!Directory.Exists(resolved))
        {
            return $"Directory not found: {displayPath}";
        }

        try
        {
            var rendered = Render(resolved, displayPath, recursive, cap, out var entryCount, out var truncated);
            LogList(_logger, workspace.AgentId, resolved, entryCount, truncated);
            return rendered;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogListFailed(_logger, workspace.AgentId, resolved, ex.Message, ex);
            return $"List failed: {ex.Message}";
        }
    }

    private static string Render(
        string root,
        string requestedPath,
        bool recursive,
        int cap,
        out int emitted,
        out bool truncated)
    {
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var directories = Directory
            .EnumerateDirectories(root, "*", option)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);
        var files = Directory
            .EnumerateFiles(root, "*", option)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase);

        var builder = new StringBuilder();
        builder.AppendFormat(CultureInfo.InvariantCulture, "Listing '{0}':", requestedPath);

        emitted = 0;
        truncated = false;

        foreach (var dir in directories)
        {
            if (emitted >= cap)
            {
                truncated = true;
                break;
            }
            var rel = Path.GetRelativePath(root, dir);
            builder.Append('\n');
            builder.Append(rel);
            builder.Append('/');
            emitted++;
        }

        if (!truncated)
        {
            foreach (var file in files)
            {
                if (emitted >= cap)
                {
                    truncated = true;
                    break;
                }
                var rel = Path.GetRelativePath(root, file);
                long size = 0;
                try
                {
                    size = new FileInfo(file).Length;
                }
                catch (IOException)
                {
                    // size 0 if the file vanished between enumerate and stat
                }
                builder.Append('\n');
                builder.AppendFormat(CultureInfo.InvariantCulture, "{0} ({1} bytes)", rel, size);
                emitted++;
            }
        }

        if (emitted == 0)
        {
            builder.Append("\n(empty)");
        }

        if (truncated)
        {
            builder.AppendFormat(
                CultureInfo.InvariantCulture,
                "\n[... truncated; exceeded {0}-entry cap. Re-call with a tighter path or a higher max_entries (max {1}) to see more.]",
                cap,
                HardMaxEntries);
        }

        return builder.ToString();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' listed {Entries} entries under '{Path}' (truncated={Truncated}).")]
    private static partial void LogList(ILogger logger, string? agentId, string path, int entries, bool truncated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "List failed for agent '{AgentId}' path '{Path}': {Message}")]
    private static partial void LogListFailed(ILogger logger, string? agentId, string path, string message, Exception ex);
}
