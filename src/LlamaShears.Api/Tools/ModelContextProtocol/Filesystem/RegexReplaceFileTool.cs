using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class RegexReplaceFileTool
{
    private const int MaxFileBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly ILogger<RegexReplaceFileTool> _logger;

    public RegexReplaceFileTool(IAgentWorkspaceLocator workspace, ILogger<RegexReplaceFileTool> logger)
    {
        _workspace = workspace;
        _logger = logger;
    }

    [McpServerTool(Name = "regex_replace_file")]
    [Description("Edits a file in place by applying a .NET regex replacement. Returns the number of replacements made. Files in the protected 'system/' subfolder cannot be edited. Hard-capped to files <= 4 MiB.")]
    public async Task<string> RegexReplaceFile(
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
            return "pattern is required.";
        }
        replacement ??= string.Empty;

        var workspace = await _workspace.GetAsync(cancellationToken).ConfigureAwait(false);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return resolution.Error;
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return $"Refused: '{path}' is a directory.";
        }
        if (!File.Exists(resolution.FullPath))
        {
            return $"File not found: {path}";
        }

        var info = new FileInfo(resolution.FullPath);
        if (info.Length > MaxFileBytes)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "Refused: file is {0} bytes; the regex-replace cap is {1} bytes.",
                info.Length,
                MaxFileBytes);
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
            return $"Invalid regex: {ex.Message}";
        }

        try
        {
            var original = await File.ReadAllTextAsync(resolution.FullPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
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
                return $"Regex timed out after {ex.MatchTimeout.TotalSeconds:N0}s; tighten the pattern.";
            }

            if (count == 0)
            {
                LogReplace(_logger, workspace.AgentId, resolution.FullPath, count);
                return $"No matches in '{path}'.";
            }

            await File.WriteAllTextAsync(resolution.FullPath, updated, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            LogReplace(_logger, workspace.AgentId, resolution.FullPath, count);
            return string.Format(
                CultureInfo.InvariantCulture,
                "Replaced {0} match(es) in '{1}'.",
                count,
                path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LogReplaceFailed(_logger, workspace.AgentId, resolution.FullPath, ex.Message, ex);
            return $"Replace failed: {ex.Message}";
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' applied regex replace to '{Path}' ({Count} matches).")]
    private static partial void LogReplace(ILogger logger, string? agentId, string path, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Regex replace failed for agent '{AgentId}' path '{Path}': {Message}")]
    private static partial void LogReplaceFailed(ILogger logger, string? agentId, string path, string message, Exception ex);
}
