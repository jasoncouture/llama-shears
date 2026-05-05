using System.Collections.Immutable;
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

    public async ValueTask<ImmutableArray<ModelContextProtocolToolSet>> DiscoverAsync(
        IReadOnlyDictionary<string, Uri> servers,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(servers);
        if (servers.Count == 0)
        {
            return [];
        }

        var builder = ImmutableArray.CreateBuilder<ModelContextProtocolToolSet>(servers.Count);
        foreach (var (name, uri) in servers)
        {
            var toolset = await DiscoverServerAsync(name, uri, cancellationToken).ConfigureAwait(false);
            if (toolset is not null)
            {
                builder.Add(toolset);
            }
        }
        return builder.ToImmutable();
    }

    private async Task<ModelContextProtocolToolSet?> DiscoverServerAsync(
        string serverName,
        Uri serverUri,
        CancellationToken cancellationToken)
    {
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
            return new ModelContextProtocolToolSet(
                ServerName: serverName,
                ServerUri: serverUri,
                Tools: [.. tools.Select(MapTool)]);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogServerDiscoveryFailed(_logger, serverName, serverUri, ex.Message, ex);
            return null;
        }
        finally
        {
            httpClient.Dispose();
        }
    }

    private static ToolDescriptor MapTool(McpClientTool tool) =>
        new(tool.Name, tool.Description ?? string.Empty, []);

    [LoggerMessage(Level = LogLevel.Warning, Message = "MCP discovery failed for server '{ServerName}' at {ServerUri}: {Message}")]
    private static partial void LogServerDiscoveryFailed(ILogger logger, string serverName, Uri serverUri, string message, Exception ex);
}
