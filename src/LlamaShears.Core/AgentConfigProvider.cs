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

    public AgentConfig? GetConfig(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var path = Path.Combine(_paths.GetPath(PathKind.Agents), agentId + ".json");
        var state = new ParseState(agentId, path, _logger);

        try
        {
            return _cache.GetOrParseAsync<AgentConfig, ParseState>(
                path,
                state,
                ParseAsync,
                CancellationToken.None).GetAwaiter().GetResult();
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

        return ValueTask.FromResult<AgentConfig?>(config);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}' from {Path}: {Message}")]
    private static partial void LogParseFailure(ILogger logger, string agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}' from {Path}: empty document")]
    private static partial void LogEmptyConfig(ILogger logger, string agentId, string path);

    private readonly record struct ParseState(string AgentId, string Path, ILogger Logger);
}
