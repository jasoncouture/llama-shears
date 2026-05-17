using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolClient : IModelContextProtocolClient
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IMemoryCache _cache;

    private record ToolListCacheEntry(ImmutableArray<ToolDescriptor>? Tools = null, ExceptionDispatchInfo? ExceptionDispatchInfo = null);

    public ModelContextProtocolClient(HttpClient httpClient, IMemoryCache cache, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
        _cache = cache;
    }

    public async ValueTask<ImmutableArray<ToolDescriptor>> ListToolsAsync(
        string serverName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        var key = $"mcp:{serverName}:tool_list";
        if (_cache.TryGetValue<ToolListCacheEntry>(key, out var cached) && cached is not null)
        {
            if (cached.Tools is not null) return cached.Tools.Value;
            if (cached.ExceptionDispatchInfo is { } replay)
            {
                throw new InvalidOperationException(
                    $"Cached MCP tool-list failure for server '{serverName}'.",
                    replay.SourceException);
            }
        }

        try
        {
            await using var client = await ConnectAsync(serverName, cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

            var result = tools.Select(MapTool).ToImmutableArray();
            _cache.Set(key, new ToolListCacheEntry(Tools: result),
                new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                });
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
            _cache.Set(key, new ToolListCacheEntry(ExceptionDispatchInfo: exceptionDispatchInfo),
                new MemoryCacheEntryOptions()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1),
                    SlidingExpiration = TimeSpan.FromSeconds(15)
                });
            throw;
        }
    }

    public async ValueTask<ToolCallResult> CallToolAsync(
        string serverName,
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);

        await using var client = await ConnectAsync(serverName, cancellationToken);
        var result = await client.CallToolAsync(
            toolName,
            arguments,
            cancellationToken: cancellationToken);
        return new ToolCallResult(
            FlattenContent(result.Content),
            IsError: result.IsError ?? false);
    }

    private Task<McpClient> ConnectAsync(string serverName, CancellationToken cancellationToken)
    {
        var endpoint = new Uri($"http://{serverName}/");
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = endpoint, Name = serverName },
            _httpClient,
            ownsHttpClient: false);
        return McpClient.CreateAsync(
            transport,
            clientOptions: null,
            loggerFactory: _loggerFactory,
            cancellationToken: cancellationToken);
    }

    private static ToolDescriptor MapTool(McpClientTool tool)
    {
        // Clone so the JsonElement survives once the MCP client's
        // backing JsonDocument is disposed below.
        var schema = tool.JsonSchema.Clone();
        return new ToolDescriptor(
            tool.Name,
            tool.Description ?? string.Empty,
            ParseSchema(schema),
            schema);
    }

    private static ImmutableArray<ToolParameter> ParseSchema(JsonElement schema)
    {
        if (schema.ValueKind != JsonValueKind.Object)
        {
            return [];
        }
        if (!schema.TryGetProperty("properties", out var properties)
            || properties.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var requiredSet = new HashSet<string>(StringComparer.Ordinal);
        if (schema.TryGetProperty("required", out var required)
            && required.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in required.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && item.GetString() is { } name)
                {
                    requiredSet.Add(name);
                }
            }
        }

        var builder = ImmutableArray.CreateBuilder<ToolParameter>();
        foreach (var property in properties.EnumerateObject())
        {
            var type = "object";
            var description = string.Empty;
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                if (property.Value.TryGetProperty("type", out var typeNode)
                    && typeNode.ValueKind == JsonValueKind.String)
                {
                    type = typeNode.GetString() ?? "object";
                }
                if (property.Value.TryGetProperty("description", out var descriptionNode)
                    && descriptionNode.ValueKind == JsonValueKind.String)
                {
                    description = descriptionNode.GetString() ?? string.Empty;
                }
            }
            builder.Add(new ToolParameter(
                property.Name,
                description,
                type,
                requiredSet.Contains(property.Name)));
        }
        return builder.ToImmutable();
    }

    private static string FlattenContent(IList<ContentBlock> blocks)
    {
        if (blocks.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var block in blocks)
        {
            if (block is TextContentBlock text)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }
                builder.Append(text.Text);
            }
        }
        return builder.ToString();
    }
}
