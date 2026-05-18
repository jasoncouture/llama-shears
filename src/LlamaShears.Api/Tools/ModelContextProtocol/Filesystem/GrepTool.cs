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
public sealed partial class GrepTool
{
    private const int DefaultMaxMatches = 200;
    private const int HardMaxMatches = 1000;
    private const long PerFileByteCap = 8L * 1024 * 1024;
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<GrepTool> _logger;

    public GrepTool(
        IAgentWorkspaceLocator workspace,
        IFileProtectionPolicy protection,
        ILogger<GrepTool> logger)
    {
        _workspace = workspace;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_grep")]
    [Description("Searches the agent's workspace for a regex across files matching a path glob. Returns a JSON object with the glob, files-scanned and match counts, a truncation flag with the applied cap, and an array of matches (each carries workspace-relative path, 1-based line and column, and the full matched line). On failure the error field is populated and matches is empty.")]
    public async Task<GrepResult> Grep(
        [Description(".NET regex pattern to match against each line.")] string pattern,
        [Description("Path glob (Microsoft.Extensions.FileSystemGlobbing syntax, e.g. '**/*.cs') anchored at the workspace root. Defaults to '**/*'.")] string pathGlob = "**/*",
        [Description("If true, match case-insensitively. Defaults to false.")] bool caseInsensitive = false,
        [Description("Maximum number of matches to return. Defaults to 200; hard-capped at 1000.")] int maxMatches = DefaultMaxMatches,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return Failure(pathGlob, DefaultMaxMatches, "pattern is required.");
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
            return Failure(pathGlob, cap, $"Invalid regex: {ex.Message}");
        }

        var workspace = await _workspace.GetAsync(cancellationToken);
        if (!Directory.Exists(workspace.Root))
        {
            return Failure(pathGlob, cap, $"Workspace not found: {workspace.Root}");
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
            if (size > PerFileByteCap)
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

    private static GrepResult Failure(string pathGlob, int cap, string error)
        => new(
            PathGlob: pathGlob,
            FilesScanned: 0,
            MatchCount: 0,
            Truncated: false,
            Cap: cap,
            Matches: [],
            Error: error);

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
            FileShare.ReadWrite,
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

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' grep glob '{Glob}' scanned {Files} files, {Matches} matches (truncated={Truncated}).")]
    private partial void LogGrep(string? agentId, string glob, int files, int matches, bool truncated);
}
