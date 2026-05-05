using System.Collections.Immutable;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Context;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolToolDiscovery : IModelContextProtocolToolDiscovery
{
    internal const string HttpClientName = "LlamaShears.ModelContextProtocol";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ModelContextProtocolToolDiscovery> _logger;

    public ModelContextProtocolToolDiscovery(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ModelContextProtocolToolDiscovery>();
    }

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
            var group = await DiscoverServerAsync(name, uri, cancellationToken).ConfigureAwait(false);
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
        // HttpClientFactory pools the underlying handler; the returned
        // HttpClient is a cheap wrapper and is not disposed here.
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        try
        {
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions
                {
                    Endpoint = serverUri,
                    Name = serverName,
                },
                httpClient,
                _loggerFactory,
                ownsHttpClient: false);
            await using var client = await McpClient.CreateAsync(
                transport,
                clientOptions: null,
                loggerFactory: _loggerFactory,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
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
        new(tool.Name, tool.Description ?? string.Empty, ParseSchema(tool.JsonSchema));

    // Pull parameter names, types, descriptions, and required-set out of
    // the MCP tool's JSON Schema so the model sees the same parameter
    // names the C# server-side binder expects. Without this the model
    // guesses arg names from the description text (often snake_case)
    // and the binder silently falls through to defaults when the casing
    // doesn't line up.
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
