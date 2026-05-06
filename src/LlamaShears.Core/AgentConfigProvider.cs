using System.Collections.Concurrent;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentConfigProvider : IAgentConfigProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IShearsPaths _paths;
    private readonly ILogger<AgentConfigProvider> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public AgentConfigProvider(IShearsPaths paths, ILogger<AgentConfigProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(paths);
        ArgumentNullException.ThrowIfNull(logger);
        _paths = paths;
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
        if (!File.Exists(path))
        {
            _cache.TryRemove(agentId, out _);
            return null;
        }

        var info = new FileInfo(path);
        var fingerprint = new Fingerprint(info.LastWriteTimeUtc, info.Length);

        if (_cache.TryGetValue(agentId, out var entry) && entry.Fingerprint == fingerprint)
        {
            return entry.Config;
        }

        AgentConfig? config;
        try
        {
            using var stream = File.OpenRead(path);
            config = JsonSerializer.Deserialize<AgentConfig>(stream, _jsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            LogParseFailure(_logger, agentId, path, ex.Message, ex);
            return null;
        }

        if (config is null)
        {
            LogEmptyConfig(_logger, agentId, path);
            return null;
        }

        _cache[agentId] = new CacheEntry(fingerprint, config);
        return config;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}' from {Path}: {Message}")]
    private static partial void LogParseFailure(ILogger logger, string agentId, string path, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}' from {Path}: empty document")]
    private static partial void LogEmptyConfig(ILogger logger, string agentId, string path);

    private readonly record struct Fingerprint(DateTime LastWriteTimeUtc, long Length);

    private sealed record CacheEntry(Fingerprint Fingerprint, AgentConfig Config);
}
