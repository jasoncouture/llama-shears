using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolClient : IModelContextProtocolClient
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerFactory _loggerFactory;

    public ModelContextProtocolClient(HttpClient httpClient, ILoggerFactory loggerFactory)
    {
        _httpClient = httpClient;
        _loggerFactory = loggerFactory;
    }

    public async ValueTask<ImmutableArray<ToolDescriptor>> ListToolsAsync(
        string serverName,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverName);

        await using var client = await ConnectAsync(serverName, cancellationToken);
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        return [.. tools.Select(MapTool)];
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

    private static ToolDescriptor MapTool(McpClientTool tool) =>
        new(tool.Name, tool.Description ?? string.Empty, ParseSchema(tool.JsonSchema));

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
