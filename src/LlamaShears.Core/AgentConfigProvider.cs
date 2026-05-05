using System.Text.Json;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentConfigProvider : IAgentConfigProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IShearsPaths _paths;
    private readonly IFileParserCache<AgentConfigProvider> _cache;
    private readonly ILogger<AgentConfigProvider> _logger;

    public AgentConfigProvider(
        IShearsPaths paths,
        IFileParserCache<AgentConfigProvider> cache,
        ILogger<AgentConfigProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);
        _paths = paths;
        _cache = cache;
        _logger = logger;
    }

    public IReadOnlyList<string> ListAgentIds()
    {
        var directory = new DirectoryInfo(_paths.GetPath(PathKind.Agents));
        if (!directory.Exists)
        {
            return [];
        }

        return [.. directory.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly)
            .Select(f => Path.GetFileNameWithoutExtension(f.Name))
            .OrderBy(static name => name, StringComparer.Ordinal)];
    }

    public async ValueTask<AgentConfig?> GetConfigAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var path = Path.Combine(_paths.GetPath(PathKind.Agents), agentId + ".json");
        var state = new ParseState(agentId, path, _logger, _paths);

        try
        {
            return await _cache.GetOrParseAsync<AgentConfig, ParseState>(
                path,
                state,
                ParseAsync,
                cancellationToken).ConfigureAwait(false);
        }
        catch (IOException ex)
        {
            LogParseFailure(_logger, agentId, path, ex.Message, ex);
            return null;
        }
    }

    private static ValueTask<AgentConfig?> ParseAsync(Stream? stream, ParseState state, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            return ValueTask.FromResult<AgentConfig?>(null);
        }

        AgentConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<AgentConfig>(stream, _jsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            LogParseFailure(state.Logger, state.AgentId, state.Path, ex.Message, ex);
            return ValueTask.FromResult<AgentConfig?>(null);
        }

        if (config is null)
        {
            LogEmptyConfig(state.Logger, state.AgentId, state.Path);
            return ValueTask.FromResult<AgentConfig?>(null);
        }

        return ValueTask.FromResult<AgentConfig?>(config with
        {
            Id = state.AgentId,
            WorkspacePath = ResolveWorkspacePath(config.WorkspacePath, state),
        });
    }

    private static string ResolveWorkspacePath(string? configured, ParseState state)
    {
        string resolved;
        if (string.IsNullOrWhiteSpace(configured))
        {
            resolved = state.Paths.GetPath(PathKind.Workspace, state.AgentId);
        }
        else if (TryExpandHomeTilde(configured, out var expanded))
        {
            resolved = expanded;
        }
        else if (Path.IsPathRooted(configured))
        {
            resolved = configured;
        }
        else
        {
            resolved = Path.Combine(state.Paths.GetPath(PathKind.Data), configured);
        }

        Directory.CreateDirectory(resolved);
        return EnsureTrailingSeparator(resolved);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path;
        }
        return $"{path}{Path.DirectorySeparatorChar}";
    }

    private static bool TryExpandHomeTilde(string path, out string expanded)
    {
        if (path[0] != '~')
        {
            expanded = path;
            return false;
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (path.Length == 1)
        {
            expanded = home;
            return true;
        }
        if (path[1] == '/' || path[1] == '\\')
        {
            expanded = Path.Combine(home, path[2..]);
            return true;
        }
        expanded = path;
        return false;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}' from {Path}: {Message}")]
    private static partial void LogParseFailure(ILogger logger, string agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}' from {Path}: empty document")]
    private static partial void LogEmptyConfig(ILogger logger, string agentId, string path);

    private readonly record struct ParseState(string AgentId, string Path, ILogger Logger, IShearsPaths Paths);
}
