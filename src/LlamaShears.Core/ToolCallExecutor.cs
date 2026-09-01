using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class ToolCallExecutor
{
    public const int ConcurrentToolCallLimit = 15;

    private readonly IEventBus _eventPublisher;
    private readonly IToolCallDispatcher _dispatcher;
    private readonly TimeProvider _time;
    private readonly ILogger<ToolCallExecutor> _logger;

    public ToolCallExecutor(
        IEventBus eventPublisher,
        IToolCallDispatcher dispatcher,
        TimeProvider time,
        ILogger<ToolCallExecutor> logger)
    {
        _eventPublisher = eventPublisher;
        _dispatcher = dispatcher;
        _time = time;
        _logger = logger;
    }

    public async Task<ImmutableArray<ModelTurn>> ExecuteAsync(
        ImmutableArray<ToolCall> calls,
        ImmutableArray<ToolGroup> tools,
        SessionId sessionId,
        Guid correlationId,
        string? channelId,
        Guid? turnSessionId,
        CancellationToken dispatchToken,
        CancellationToken publishToken)
    {
        ArgumentNullException.ThrowIfNull(sessionId);
        if (calls.IsDefaultOrEmpty)
        {
            return [];
        }

        var turns = new List<ModelTurn>(calls.Length);
        for (var i = 0; i < calls.Length; i++)
        {
            var call = calls[i];
            var result = i >= ConcurrentToolCallLimit
                ? await LimitAsync(call, sessionId, correlationId, publishToken)
                : await _dispatcher.DispatchAsync(call, tools, sessionId, correlationId, dispatchToken);
            var turn = new ModelTurn(ModelRole.Tool, result.Content, _time.GetLocalNow(), ChannelId: channelId)
            {
                ToolCall = call,
                IsError = result.IsError,
                SessionId = turnSessionId,
            };
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = sessionId },
                turn,
                correlationId,
                publishToken);
            turns.Add(turn);
        }

        return [.. turns];
    }

    private async Task<ToolCallResult> LimitAsync(
        ToolCall call,
        SessionId sessionId,
        Guid correlationId,
        CancellationToken publishToken)
    {
        LogToolCallLimitExceeded(call.Source, call.Name, ConcurrentToolCallLimit);
        var result = new ToolCallResult(
            $"Tool call limit exceeded, concurrent tool calls are limited to {ConcurrentToolCallLimit}",
            IsError: true);
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.ToolResult with { Id = sessionId },
            new AgentToolResultFragment(call.Source, call.Name, result.Content, result.IsError, call.CallId),
            correlationId,
            publishToken);
        return result;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tool call '{Source}.{Name}' refused: limit of {Limit} per turn exceeded.")]
    private partial void LogToolCallLimitExceeded(string source, string name, int limit);
}
