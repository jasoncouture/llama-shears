using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentConfigProvider : IAgentConfigProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IApplicationPathProvider _paths;
    private readonly IFileParserCache<AgentConfigProvider> _cache;
    private readonly ILogger<AgentConfigProvider> _logger;

    public AgentConfigProvider(
        IApplicationPathProvider paths,
        IFileParserCache<AgentConfigProvider> cache,
        ILogger<AgentConfigProvider> logger)
    {
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
        var state = new ParseState(agentId, path, _logger);

        AgentConfig? raw;
        try
        {
            raw = await _cache.GetOrParseAsync(
                path,
                state,
                ParseAsync,
                cancellationToken);
        }
        catch (IOException ex)
        {
            LogParseFailure(_logger, agentId, path, ex.Message, ex);
            return null;
        }

        return raw is null ? null : Stamp(raw, agentId);
    }

    public async ValueTask<AgentConfigFile?> ReadFileAsync(string agentId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);

        var path = Path.Combine(_paths.GetPath(PathKind.Agents), agentId + ".json");
        if (!File.Exists(path))
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(bytes));
        return new AgentConfigFile(Encoding.UTF8.GetString(bytes), hash);
    }

    public async ValueTask<SaveAgentConfigResult> SaveAsync(
        string agentId,
        string expectedHash,
        string content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHash);
        ArgumentNullException.ThrowIfNull(content);

        var path = Path.Combine(_paths.GetPath(PathKind.Agents), agentId + ".json");

        var currentHash = string.Empty;
        if (File.Exists(path))
        {
            var existing = await File.ReadAllBytesAsync(path, cancellationToken);
            currentHash = Convert.ToHexString(SHA256.HashData(existing));
        }
        if (!string.Equals(expectedHash, currentHash, StringComparison.OrdinalIgnoreCase))
        {
            return new SaveAgentConfigResult.Conflict(currentHash);
        }

        var bytes = Encoding.UTF8.GetBytes(content);
        AgentConfig? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<AgentConfig>(bytes, _jsonOptions);
        }
        catch (JsonException ex)
        {
            return new SaveAgentConfigResult.InvalidJson(ex.Message);
        }
        if (parsed is null)
        {
            return new SaveAgentConfigResult.InvalidJson("Document was empty.");
        }

        if (File.Exists(path))
        {
            var unixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var backupPath = $"{path}.{unixSeconds}.bak";
            File.Copy(path, backupPath, overwrite: false);
            PruneOldBackups(path);
        }

        var tempPath = path + ".tmp";
        await File.WriteAllBytesAsync(tempPath, bytes, cancellationToken);
        File.Move(tempPath, path, overwrite: true);

        var newHash = Convert.ToHexString(SHA256.HashData(bytes));
        return new SaveAgentConfigResult.Ok(newHash);
    }

    private const int BackupRetentionCount = 10;

    private static void PruneOldBackups(string configPath)
    {
        var directory = Path.GetDirectoryName(configPath);
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }
        var prefix = Path.GetFileName(configPath) + ".";
        var stale = Directory.EnumerateFiles(directory, prefix + "*.bak")
            .Select(static p => (Path: p, Timestamp: ParseBackupTimestamp(p)))
            .OrderByDescending(static entry => entry.Timestamp)
            .Skip(BackupRetentionCount)
            .Select(static entry => entry.Path);
        foreach (var path in stale)
        {
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
        }
    }

    private static long ParseBackupTimestamp(string fullPath)
    {
        var fileName = Path.GetFileName(fullPath);
        if (!fileName.EndsWith(".bak", StringComparison.Ordinal))
        {
            return 0;
        }
        var withoutBak = fileName.AsSpan(0, fileName.Length - ".bak".Length);
        var lastDot = withoutBak.LastIndexOf('.');
        if (lastDot < 0)
        {
            return 0;
        }
        return long.TryParse(withoutBak[(lastDot + 1)..], out var ts) ? ts : 0;
    }

    private AgentConfig Stamp(AgentConfig raw, string agentId) =>
        raw with
        {
            Id = agentId,
            WorkspacePath = ResolveWorkspacePath(raw.WorkspacePath, agentId),
        };

    private static async ValueTask<AgentConfig?> ParseAsync(Stream? stream, ParseState state, CancellationToken cancellationToken)
    {
        if (stream is null)
        {
            return null;
        }

        byte[] bytes;
        try
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            bytes = buffer.ToArray();
        }
        catch (IOException ex)
        {
            LogParseFailure(state.Logger, state.AgentId, state.Path, ex.Message, ex);
            return null;
        }

        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        AgentConfig? config;
        try
        {
            config = JsonSerializer.Deserialize<AgentConfig>(bytes, _jsonOptions);
        }
        catch (JsonException ex)
        {
            LogParseFailure(state.Logger, state.AgentId, state.Path, ex.Message, ex);
            return null;
        }

        if (config is null)
        {
            LogEmptyConfig(state.Logger, state.AgentId, state.Path);
            return null;
        }

        return config with { Hash = hash };
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
