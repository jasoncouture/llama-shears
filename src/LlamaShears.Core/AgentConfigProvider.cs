using System.Collections.Immutable;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentConfigProvider : IAgentConfigProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private const string InternalServerName = "llamashears";

    private readonly IShearsPaths _paths;
    private readonly IFileParserCache<AgentConfigProvider> _cache;
    private readonly ILogger<AgentConfigProvider> _logger;
    private readonly IInternalModelContextProtocolServer _internalMcpServer;

    public AgentConfigProvider(
        IShearsPaths paths,
        IFileParserCache<AgentConfigProvider> cache,
        ILogger<AgentConfigProvider> logger,
        IInternalModelContextProtocolServer internalMcpServer)
    {
        _paths = paths;
        _cache = cache;
        _logger = logger;
        _internalMcpServer = internalMcpServer;
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
        var state = new ParseState(agentId, path, _logger);

        AgentConfig? raw;
        try
        {
            raw = await _cache.GetOrParseAsync<AgentConfig, ParseState>(
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

        return raw is null ? null : Stamp(raw, agentId);
    }

    private AgentConfig Stamp(AgentConfig raw, string agentId)
    {
        var userServers = raw.ModelContextProtocolServers ?? [];
        var internalUri = _internalMcpServer.Uri;
        var servers = internalUri is null
            ? userServers
            : userServers.SetItem(InternalServerName, internalUri);

        return raw with
        {
            Id = agentId,
            WorkspacePath = ResolveWorkspacePath(raw.WorkspacePath, agentId),
            ModelContextProtocolServers = servers,
        };
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

        return ValueTask.FromResult<AgentConfig?>(config);
    }

    private string ResolveWorkspacePath(string? configured, string agentId)
    {
        string resolved;
        if (string.IsNullOrWhiteSpace(configured))
        {
            resolved = _paths.GetPath(PathKind.Workspace, agentId);
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
            resolved = Path.Combine(_paths.GetPath(PathKind.Data), configured);
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

    private readonly record struct ParseState(string AgentId, string Path, ILogger Logger);
}
