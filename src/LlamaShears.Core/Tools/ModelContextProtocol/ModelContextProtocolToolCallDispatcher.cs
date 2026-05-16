using System.Collections.Immutable;
using System.Text.Json;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolToolCallDispatcher : IToolCallDispatcher
{
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private readonly IModelContextProtocolClient _client;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ModelContextProtocolToolCallDispatcher> _logger;

    public ModelContextProtocolToolCallDispatcher(
        IModelContextProtocolClient client,
        IEventPublisher eventPublisher,
        ILogger<ModelContextProtocolToolCallDispatcher> logger)
    {
        _client = client;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async ValueTask<ToolCallResult> DispatchAsync(
        ToolCall call,
        ImmutableArray<ToolGroup> tools,
        string eventId,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(call);
        ArgumentException.ThrowIfNullOrEmpty(eventId);

        var result = await ExecuteAsync(call, tools, cancellationToken);
        await PublishResultAsync(eventId, call, result, correlationId, cancellationToken);
        return result;
    }

    private async ValueTask<ToolCallResult> ExecuteAsync(
        ToolCall call,
        ImmutableArray<ToolGroup> tools,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(call.Source))
        {
            LogMissingSource(call.Name);
            return new ToolCallResult(
                $"Tool '{call.Name}' was rejected: no server was specified.",
                IsError: true);
        }

        if (!IsAdvertised(tools, call.Source, call.Name))
        {
            LogNotAdvertised(call.Source, call.Name);
            return new ToolCallResult(
                $"Tool '{call.Name}' on server '{call.Source}' was rejected: not advertised on this turn.",
                IsError: true);
        }

        var arguments = DeserializeArguments(call.ArgumentsJson);

        try
        {
            var result = await _client.CallToolAsync(
                call.Source,
                call.Name,
                arguments,
                cancellationToken);
            return result with { Content = ToolResponseClamp.Apply(result.Content) };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new ToolCallResult(
                $"Tool '{call.Name}' on server '{call.Source}' was interrupted by user.",
                IsError: true);
        }
        catch (Exception ex)
        {
            LogDispatchFailed(call.Source, call.Name, ex.Message, ex);
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
            publishToken);
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing tool call '{Name}': no server was specified.")]
    private partial void LogMissingSource(string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refusing tool '{Name}' on server '{Source}': not advertised on this turn.")]
    private partial void LogNotAdvertised(string source, string name);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tool '{Name}' on server '{Source}' failed: {Message}")]
    private partial void LogDispatchFailed(string source, string name, string message, Exception ex);
}
