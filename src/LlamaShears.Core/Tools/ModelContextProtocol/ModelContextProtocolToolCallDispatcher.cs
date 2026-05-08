using System.Text;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Caching;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolToolCallDispatcher : IToolCallDispatcher, IAsyncDisposable
{
    private static readonly TimeSpan _connectionIdleTimeout = TimeSpan.FromMinutes(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IModelContextProtocolServerRegistry _serverRegistry;
    private readonly ILogger<ModelContextProtocolToolCallDispatcher> _logger;
    private readonly LifetimeCache<(string Name, Uri Endpoint), McpClient> _connections;

    public ModelContextProtocolToolCallDispatcher(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IModelContextProtocolServerRegistry serverRegistry)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _serverRegistry = serverRegistry;
        _logger = loggerFactory.CreateLogger<ModelContextProtocolToolCallDispatcher>();
        _connections = new LifetimeCache<(string, Uri), McpClient>(ConnectAsync, _connectionIdleTimeout);
    }

    private Task<McpClient> ConnectAsync((string Name, Uri Endpoint) key, CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient(nameof(ModelContextProtocolToolCallDispatcher));
        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = key.Endpoint, Name = key.Name },
            httpClient,
            _loggerFactory,
            ownsHttpClient: false);
        return McpClient.CreateAsync(transport, clientOptions: null, loggerFactory: _loggerFactory, cancellationToken: cancellationToken);
    }

    public ValueTask DisposeAsync() => _connections.DisposeAsync();

    public async ValueTask<ToolCallResult> DispatchAsync(ToolCall call, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);

        if (string.IsNullOrEmpty(call.Source))
        {
            LogMissingSource(_logger, call.Name);
            return new ToolCallResult(
                $"Tool call '{call.Name}' was rejected: the model emitted an unprefixed name and the host can't route it to a server.",
                IsError: true);
        }

        var servers = _serverRegistry.Resolve(whitelist: [call.Source]);
        if (!servers.TryGetValue(call.Source, out var serverUri))
        {
            LogUnknownSource(_logger, call.Source, call.Name);
            return new ToolCallResult(
                $"Tool call '{call.Source}__{call.Name}' was rejected: server '{call.Source}' is not registered.",
                IsError: true);
        }

        var arguments = DeserializeArguments(call.ArgumentsJson);

        try
        {
            var client = await _connections.GetOrCreateAsync((call.Source, serverUri), cancellationToken).ConfigureAwait(false);
            var result = await client.CallToolAsync(
                call.Name,
                arguments,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return new ToolCallResult(
                FlattenContent(result.Content),
                IsError: result.IsError ?? false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogDispatchFailed(_logger, call.Source, call.Name, ex.Message, ex);
            return new ToolCallResult(
                $"Tool call '{call.Source}__{call.Name}' failed: {ex.Message}",
                IsError: true);
        }
    }

    private static IReadOnlyDictionary<string, object?>? DeserializeArguments(string argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing tool call '{Name}': model emitted no source prefix.")]
    private static partial void LogMissingSource(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing tool call '{Source}__{Name}': source '{Source}' is not a registered MCP server.")]
    private static partial void LogUnknownSource(ILogger logger, string source, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tool call '{Source}__{Name}' failed: {Message}")]
    private static partial void LogDispatchFailed(ILogger logger, string source, string name, string message, Exception ex);
}
