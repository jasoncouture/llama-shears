using System.Collections.Immutable;
using System.Text;
using LlamaShears.Core.Abstractions;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class InferenceRunner : IInferenceRunner
{
    private const int ConsecutiveToolCallLimit = 15;
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private readonly IEventBus _eventPublisher;
    private readonly IToolCallDispatcher _toolDispatcher;
    private readonly TimeProvider _time;
    private readonly IDataContextScope _dataScope;
    private readonly ILanguageModel _model;
    private readonly ILogger<InferenceRunner> _logger;

    public InferenceRunner(
        IEventBus eventPublisher,
        IToolCallDispatcher toolDispatcher,
        TimeProvider time,
        IDataContextScope dataScope,
        ILanguageModel model,
        ILogger<InferenceRunner> logger)
    {
        _eventPublisher = eventPublisher;
        _toolDispatcher = toolDispatcher;
        _time = time;
        _dataScope = dataScope;
        _model = model;
        _logger = logger;
    }

    public async Task<InferenceOutcome> RunAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (prompt.Turns.Count == 0)
        {
            throw new ArgumentException("Prompt must contain at least one turn.", nameof(prompt));
        }

        var state = _dataScope.GetAgentState();
        var sessionId = _dataScope.GetCurrentSessionId();
        var eventId = state.EventId;
        var correlationId = state.CorrelationId;
        var channelId = prompt.Turns[^1].ChannelId;
        var emitTurns = options?.EmitTurns ?? false;

        var thinking = new StringBuilder();
        var content = new StringBuilder();
        int? tokenCount = null;
        var toolCalls = ImmutableArray.CreateBuilder<ToolCall>();

        var tools = options?.Tools ?? [];
        var dispatchTasks = new List<Task<ToolCallResult>>();
        var interrupted = false;
        var textSuppressed = true;

        var errorStateCancellationToken = new CancellationTokenSource();

        try
        {
            await foreach (var fragment in _model.PromptAsync(prompt, options, cancellationToken).ConfigureAwait(false))
            {
                switch (fragment)
                {
                    case IModelThoughtResponse thought:
                        thinking.Append(thought.Content);
                        await PublishModelFragment(ModelRole.Thought, new AgentThoughtFragment(thinking.ToString(), ChannelId: channelId, Final: false), cancellationToken);
                        break;
                    case IModelTextResponse text:
                        content.Append(text.Content);
                        if (textSuppressed)
                        {
                            var snapshot = content.ToString();
                            if (snapshot.Length <= Sentinel.NoResponse.Length
                                && Sentinel.NoResponse.StartsWith(snapshot, StringComparison.Ordinal))
                            {
                                break;
                            }
                            textSuppressed = false;
                        }
                        await PublishModelFragment(ModelRole.Assistant, new AgentMessageFragment(content.ToString(), ChannelId: channelId, Final: false), cancellationToken);
                        break;
                    case IModelToolCallFragment toolFragment:
                        LogToolCall(toolFragment.Call.Source, toolFragment.Call.Name, toolFragment.Call.CallId, toolFragment.Call.ArgumentsJson);
                        toolCalls.Add(toolFragment.Call);
                        var agentToolCallFragment = new AgentToolCallFragment(
                                toolFragment.Call.Source,
                                toolFragment.Call.Name,
                                toolFragment.Call.ArgumentsJson,
                                toolFragment.Call.CallId);
                        await PublishModelFragment(ModelRole.Tool, agentToolCallFragment, cancellationToken);
                        var predecessor = dispatchTasks.Count > 0 ? dispatchTasks[^1] : Task.CompletedTask;
                        if (toolCalls.Count > ConsecutiveToolCallLimit)
                        {
                            dispatchTasks.Add(EnqueueLimitedAsync(predecessor, toolFragment.Call));
                        }
                        else
                        {
                            dispatchTasks.Add(EnqueueDispatchAsync(predecessor, toolFragment.Call));
                        }
                        break;
                    case IModelCompletionResponse completion:
                        tokenCount = completion.TokenCount;
                        break;
                }
            }
            // Wait for any dangling tasks, and publish them.
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            interrupted = true;
            cancellationToken = errorStateCancellationToken.Token;
            errorStateCancellationToken.CancelAfter(_interruptFinalizeTimeout);
        }
        finally
        {
            await Task.WhenAll(dispatchTasks);
        }


        if (thinking.Length > 0)
        {
            await PublishModelFragment(ModelRole.Thought, new AgentThoughtFragment(thinking.ToString(), ChannelId: channelId, Final: true), cancellationToken);
            if (emitTurns)
            {
                // I would refactor this too into a method, just like above.
                // but 2 instances isn't worth the effort.
                var thoughtTurn = new ModelTurn(ModelRole.Thought, thinking.ToString(), _time.GetLocalNow(), ChannelId: channelId);
                await _eventPublisher.PublishAsync(
                    Event.WellKnown.Agent.Turn with { Id = sessionId },
                    thoughtTurn,
                    correlationId,
                    cancellationToken);
            }
        }
        var suppressed = content.ToString() == Sentinel.NoResponse && toolCalls.Count == 0;
        var finalContent = suppressed ? string.Empty : content.ToString();
        if (finalContent.Length > 0)
        {
            await PublishModelFragment(ModelRole.Assistant, new AgentMessageFragment(finalContent, ChannelId: channelId, Final: true), cancellationToken);
        }
        if (emitTurns && (finalContent.Length > 0 || toolCalls.Count > 0))
        {
            var assistantTurn = new ModelTurn(ModelRole.Assistant, finalContent, _time.GetLocalNow(), ChannelId: channelId)
            {
                ToolCalls = toolCalls.ToImmutable(),
            };
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = sessionId },
                assistantTurn,
                correlationId,
                cancellationToken);
        }


        return new InferenceOutcome(
            thinking.ToString(),
            finalContent,
            tokenCount,
            toolCalls.ToImmutable(),
            [.. await Task.WhenAll(dispatchTasks)],
            Interrupted: interrupted,
            Suppressed: suppressed);

        Task<ToolCallResult> EnqueueDispatchAsync(Task predecessor, ToolCall call)
            => predecessor.ContinueWith(async _ =>
            {
                var result = await _toolDispatcher.DispatchAsync(call, tools, sessionId, correlationId, cancellationToken);
                await PublishCompletedToolCallAsync(channelId, result, call, cancellationToken);
                return result;
            }, cancellationToken).Unwrap();

        Task<ToolCallResult> EnqueueLimitedAsync(Task predecessor, ToolCall call)
            => predecessor.ContinueWith(async _ =>
            {
                LogToolCallLimitExceeded(call.Source, call.Name, ConsecutiveToolCallLimit);
                var result = new ToolCallResult(
                    $"Tool call limit exceeded, concurrent tool calls are limited to {ConsecutiveToolCallLimit}",
                    IsError: true);
                await _eventPublisher.PublishAsync(
                    Event.WellKnown.Agent.ToolResult with { Id = sessionId },
                    new AgentToolResultFragment(call.Source, call.Name, result.Content, result.IsError, call.CallId),
                    correlationId,
                    cancellationToken);
                await PublishCompletedToolCallAsync(channelId, result, call, cancellationToken);
                return result;
            }, cancellationToken).Unwrap();
    }

    private async Task PublishModelFragment<T>(
        ModelRole role,
        T fragment,
        CancellationToken cancellationToken) where T : class, IAgentMessage
    {
        var sessionId = _dataScope.GetCurrentSessionId();
        var state = _dataScope.GetAgentState();
        var typedEventId = role switch
        {
            ModelRole.Thought => Event.WellKnown.Agent.Thought,
            ModelRole.Assistant => Event.WellKnown.Agent.Message,
            ModelRole.Tool => Event.WellKnown.Agent.ToolCall,
            _ => throw new ArgumentException("Unknown model role", nameof(role))
        } with
        { Id = sessionId };
        await _eventPublisher.PublishAsync(
            typedEventId,
            fragment,
            state.CorrelationId,
            cancellationToken);
    }

    private async ValueTask PublishCompletedToolCallAsync(
        string? channelId,
        ToolCallResult result,
        ToolCall toolCall,
        CancellationToken cancellationToken)
    {
        var sessionId = _dataScope.GetCurrentSessionId();
        var state = _dataScope.GetAgentState();
        var toolTurn = new ModelTurn(ModelRole.Tool, result.Content, _time.GetLocalNow(), ChannelId: channelId)
        {
            ToolCall = toolCall,
            IsError = result.IsError,
        };
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.Turn with { Id = sessionId },
            toolTurn,
            state.CorrelationId,
            cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tool call received: '{Source}.{Name}' (callId={CallId}) args={Arguments}")]
    private partial void LogToolCall(string source, string name, string? callId, string arguments);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Tool call '{Source}.{Name}' refused: limit of {Limit} per turn exceeded.")]
    private partial void LogToolCallLimitExceeded(string source, string name, int limit);
}
