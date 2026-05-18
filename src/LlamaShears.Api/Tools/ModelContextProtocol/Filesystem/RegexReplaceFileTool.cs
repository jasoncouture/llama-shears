using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;

[McpServerToolType]
public sealed partial class RegexReplaceFileTool
{
    private const int MaxFileBytes = 4 * 1024 * 1024;
    private static readonly TimeSpan _regexTimeout = TimeSpan.FromSeconds(2);

    private readonly IAgentWorkspaceLocator _workspace;
    private readonly IPathExpander _pathExpander;
    private readonly IFileProtectionPolicy _protection;
    private readonly ILogger<RegexReplaceFileTool> _logger;

    public RegexReplaceFileTool(
        IAgentWorkspaceLocator workspace,
        IPathExpander pathExpander,
        IFileProtectionPolicy protection,
        ILogger<RegexReplaceFileTool> logger)
    {
        _workspace = workspace;
        _pathExpander = pathExpander;
        _protection = protection;
        _logger = logger;
    }

    [McpServerTool(Name = "file_regex_replace")]
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
            return Failure(path, "pattern is required.");
        }
        replacement ??= string.Empty;

        var workspace = await _workspace.GetAsync(cancellationToken);
        var resolution = WorkspacePathResolver.ResolveForWrite(workspace, path);
        if (!resolution.IsSuccess)
        {
            return Failure(path, resolution.Error);
        }

        if (Directory.Exists(resolution.FullPath))
        {
            return Failure(path, $"Refused: '{path}' is a directory.");
        }
        if (!File.Exists(resolution.FullPath))
        {
            return Failure(path, $"File not found: {path}");
        }

        var fullPath = _pathExpander.ExpandPath(path, workspace.Root);
        var protection = _protection.Match(workspace.Root, fullPath, FileType.File, ProtectionMode.Write);
        if (protection is not null)
        {
            return Failure(path, ProtectionRefusal.Format(path, ProtectionMode.Write, protection));
        }

        var info = new FileInfo(resolution.FullPath);
        if (info.Length > MaxFileBytes)
        {
            return Failure(path, $"Refused: file is {info.Length} bytes; the regex-replace cap is {MaxFileBytes} bytes.");
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
            return Failure(path, $"Invalid regex: {ex.Message}");
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
                return Failure(path, $"Regex timed out after {ex.MatchTimeout.TotalSeconds:N0}s; tighten the pattern.");
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
            return Failure(path, $"Replace failed: {ex.Message}");
        }
    }

    private static FileRegexReplaceResult Failure(string path, string error)
        => new(
            Path: path,
            Edited: false,
            Replacements: 0,
            Error: error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' applied regex replace to '{Path}' ({Count} matches).")]
    private partial void LogReplace(string? agentId, string path, int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Regex replace failed for agent '{AgentId}' path '{Path}': {Message}")]
    private partial void LogReplaceFailed(string? agentId, string path, string message, Exception ex);
}
