using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Caching;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolToolCallDispatcher : IToolCallDispatcher, IAsyncDisposable
{
    private static readonly TimeSpan _connectionIdleTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IModelContextProtocolServerRegistry _serverRegistry;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ModelContextProtocolToolCallDispatcher> _logger;
    private readonly LifetimeCache<(string Name, Uri Endpoint), McpClient> _connections;

    public ModelContextProtocolToolCallDispatcher(
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IModelContextProtocolServerRegistry serverRegistry,
        IEventPublisher eventPublisher)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _serverRegistry = serverRegistry;
        _eventPublisher = eventPublisher;
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

    public async ValueTask<ToolCallResult> DispatchAsync(
        ToolCall call,
        ImmutableArray<ToolGroup> tools,
        string eventId,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentException.ThrowIfNullOrEmpty(eventId);

        var result = await ExecuteAsync(call, tools, cancellationToken).ConfigureAwait(false);
        await PublishResultAsync(eventId, call, result, correlationId, cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async ValueTask<ToolCallResult> ExecuteAsync(
        ToolCall call,
        ImmutableArray<ToolGroup> tools,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(call.Source))
        {
            LogMissingSource(_logger, call.Name);
            return new ToolCallResult(
                $"Tool '{call.Name}' was rejected: no server was specified.",
                IsError: true);
        }

        if (!IsAdvertised(tools, call.Source, call.Name))
        {
            LogNotAdvertised(_logger, call.Source, call.Name);
            return new ToolCallResult(
                $"Tool '{call.Name}' on server '{call.Source}' was rejected: not advertised on this turn.",
                IsError: true);
        }

        var servers = _serverRegistry.Resolve(whitelist: [call.Source]);
        if (!servers.TryGetValue(call.Source, out var serverUri))
        {
            LogUnknownSource(_logger, call.Source, call.Name);
            return new ToolCallResult(
                $"Tool '{call.Name}' on server '{call.Source}' was rejected: server is not registered.",
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ToolCallResult(
                $"Tool '{call.Name}' on server '{call.Source}' was interrupted by user.",
                IsError: true);
        }
        catch (Exception ex)
        {
            LogDispatchFailed(_logger, call.Source, call.Name, ex.Message, ex);
            return new ToolCallResult(
                $"Tool '{call.Name}' on server '{call.Source}' failed: {ex.Message}",
                IsError: true);
        }
    }

    private async ValueTask PublishResultAsync(
        string eventId,
        ToolCall call,
        ToolCallResult result,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        using var graceTokenSource = cancellationToken.IsCancellationRequested
            ? new CancellationTokenSource(_interruptFinalizeTimeout)
            : null;
        var publishToken = graceTokenSource?.Token ?? cancellationToken;
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.ToolResult with { Id = eventId },
            new AgentToolResultFragment(
                call.Source,
                call.Name,
                result.Content,
                result.IsError,
                call.CallId),
            correlationId,
            publishToken).ConfigureAwait(false);
    }

    private static bool IsAdvertised(ImmutableArray<ToolGroup> tools, string source, string name)
    {
        if (tools.IsDefaultOrEmpty)
        {
            return false;
        }
        foreach (var group in tools)
        {
            if (!string.Equals(group.Source, source, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (var descriptor in group.Tools)
            {
                if (string.Equals(descriptor.Name, name, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }
        return false;
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing tool call '{Name}': no server was specified.")]
    private static partial void LogMissingSource(ILogger logger, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing tool '{Name}' on server '{Source}': server is not registered.")]
    private static partial void LogUnknownSource(ILogger logger, string source, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing tool '{Name}' on server '{Source}': not advertised on this turn.")]
    private static partial void LogNotAdvertised(ILogger logger, string source, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tool '{Name}' on server '{Source}' failed: {Message}")]
    private static partial void LogDispatchFailed(ILogger logger, string source, string name, string message, Exception ex);
}
