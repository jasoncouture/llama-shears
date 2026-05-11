using System.Collections.Immutable;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Caching;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolToolDiscovery : IModelContextProtocolToolDiscovery, IAsyncDisposable
{
    private static readonly TimeSpan _connectionIdleTimeout = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ModelContextProtocolToolDiscovery> _logger;
    private readonly LifetimeCache<(string Name, Uri Endpoint), McpClient> _connections;

    public ModelContextProtocolToolDiscovery(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ModelContextProtocolToolDiscovery>();
        _connections = new LifetimeCache<(string, Uri), McpClient>(ConnectAsync, _connectionIdleTimeout);
    }

    private Task<McpClient> ConnectAsync((string Name, Uri Endpoint) key, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(ModelContextProtocolToolDiscovery));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = key.Endpoint, Name = key.Name },
            httpClient,
            _loggerFactory,
            ownsHttpClient: false);
        return McpClient.CreateAsync(transport, clientOptions: null, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
    }

    public ValueTask DisposeAsync() => _connections.DisposeAsync();

    public async ValueTask<ImmutableArray<ToolGroup>> DiscoverAsync(
        IReadOnlyDictionary<string, Uri> servers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servers);
        if (servers.Count == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<ToolGroup>(servers.Count);
        foreach (var (name, uri) in servers)
        {
            var group = await DiscoverServerAsync(name, uri, cancellationToken);
            if (group is not null)
            {
                builder.Add(group);
            }
        }
        return builder.ToImmutable();
    }

    private async Task<ToolGroup?> DiscoverServerAsync(
        string serverName,
        Uri serverUri,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = await _connections.GetOrCreateAsync((serverName, serverUri), cancellationToken);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            return new ToolGroup(
                Source: serverName,
                Tools: [.. tools.Select(MapTool)]);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogServerDiscoveryFailed(_logger, serverName, serverUri, ex.Message, ex);
            return null;
        }
    }

    private static ToolDescriptor MapTool(McpClientTool tool) =>
        new ToolDescriptor(tool.Name, tool.Description ?? string.Empty, ParseSchema(tool.JsonSchema));

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "MCP discovery failed for server '{ServerName}' at {ServerUri}: {Message}")]
    private static partial void LogServerDiscoveryFailed(ILogger logger, string serverName, Uri serverUri, string message, Exception ex);
}
