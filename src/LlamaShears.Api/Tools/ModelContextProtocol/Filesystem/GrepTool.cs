using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class GrepTool
{
    private const int DefaultMaxMatches = 200;
    private const int HardMaxMatches = 1000;
    private const long PerFileByteCap = 8L * 1024 * 1024;
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<GrepTool> _logger;

    public GrepTool(IAgentWorkspaceLocator workspace, ILogger<GrepTool> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "grep")]
    [Description("Searches the agent's workspace for a regex across files matching a path glob. Returns one line per match in the form 'relative/path:line:column: matched-line'. Output is capped; a truncation marker is appended when the result exceeds the cap.")]
    public async Task<string> Grep(
        [Description(".NET regex pattern to match against each line.")] string pattern,
        [Description("Path glob (Microsoft.Extensions.FileSystemGlobbing syntax, e.g. '**/*.cs') anchored at the workspace root. Defaults to '**/*'.")] string pathGlob = "**/*",
        [Description("If true, match case-insensitively. Defaults to false.")] bool caseInsensitive = false,
        [Description("Maximum number of matches to return. Defaults to 200; hard-capped at 1000.")] int maxMatches = DefaultMaxMatches,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return "pattern is required.";
        }
        if (string.IsNullOrWhiteSpace(pathGlob))
        {
            pathGlob = "**/*";
        }
        var cap = Math.Clamp(maxMatches, 1, HardMaxMatches);

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
            return $"Invalid regex: {ex.Message}";
        }

        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!Directory.Exists(workspace.Root))
        {
            return $"Workspace not found: {workspace.Root}";
        }

        var matcher = new Matcher(StringComparison.Ordinal);
        matcher.AddInclude(pathGlob);
        var matchResult = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(workspace.Root)));
        if (!matchResult.HasMatches)
        {
            return $"No files match glob '{pathGlob}'.";
        }

        var output = new StringBuilder();
        var matchCount = 0;
        var truncated = false;
        var filesScanned = 0;

        foreach (var hit in matchResult.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (matchCount >= cap)
            {
                truncated = true;
                break;
            }

            var fullPath = Path.GetFullPath(Path.Combine(workspace.Root, hit.Path));
            if (!File.Exists(fullPath))
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
            if (size > PerFileByteCap)
            {
                continue;
            }

            filesScanned++;
            try
            {
                var fileMatches = await ScanFileAsync(fullPath, hit.Path, regex, output, cap - matchCount, cancellationToken).ConfigureAwait(false);
                matchCount += fileMatches;
            }
            catch (RegexMatchTimeoutException)
            {
                output.Append('\n');
                output.AppendFormat(CultureInfo.InvariantCulture, "[{0}: regex timeout — skipped]", hit.Path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                output.Append('\n');
                output.AppendFormat(CultureInfo.InvariantCulture, "[{0}: {1}]", hit.Path, ex.Message);
            }

            if (matchCount >= cap)
            {
                truncated = true;
                break;
            }
        }

        if (matchCount == 0)
        {
            LogGrep(_logger, workspace.AgentId, pathGlob, filesScanned, matchCount, truncated);
            return $"No matches found for pattern in glob '{pathGlob}'.";
        }

        if (truncated)
        {
            output.Append('\n');
            output.AppendFormat(
                CultureInfo.InvariantCulture,
                "[... truncated; exceeded {0}-match cap. Re-call with a tighter glob/pattern or a higher max_matches (max {1}).]",
                cap,
                HardMaxMatches);
        }

        LogGrep(_logger, workspace.AgentId, pathGlob, filesScanned, matchCount, truncated);
        return output.ToString().TrimStart('\n');
    }

    private static async Task<int> ScanFileAsync(
        string fullPath,
        string relativePath,
        Regex regex,
        StringBuilder output,
        int remaining,
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

        var lineNumber = 0;
        var emitted = 0;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineNumber++;
            var match = regex.Match(line);
            while (match.Success)
            {
                if (emitted >= remaining)
                {
                    return emitted;
                }
                output.Append('\n');
                output.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0}:{1}:{2}: {3}",
                    relativePath,
                    lineNumber,
                    match.Index + 1,
                    line);
                emitted++;
                if (match.Length == 0)
                {
                    break;
                }
                match = match.NextMatch();
            }
        }
        return emitted;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' grep glob '{Glob}' scanned {Files} files, {Matches} matches (truncated={Truncated}).")]
    private static partial void LogGrep(ILogger logger, string? agentId, string glob, int files, int matches, bool truncated);
}
