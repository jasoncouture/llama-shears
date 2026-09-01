using System.Collections.Immutable;
using System.Text;
using LlamaShears.Core.Abstractions;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class InferenceRunner : IInferenceRunner
{
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private readonly IEventBus _eventPublisher;
    private readonly TimeProvider _time;
    private readonly ILanguageModel _model;
    private readonly ILogger<InferenceRunner> _logger;

    public InferenceRunner(
        IEventBus eventPublisher,
        TimeProvider time,
        ILanguageModel model,
        ILogger<InferenceRunner> logger)
    {
        _eventPublisher = eventPublisher;
        _time = time;
        _model = model;
        _logger = logger;
    }

    public async Task<InferenceOutcome> RunAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        SessionId sessionId,
        Guid correlationId,
        string? channelId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(sessionId);

        if (prompt.Turns.Count == 0)
        {
            throw new ArgumentException("Prompt must contain at least one turn.", nameof(prompt));
        }

        var emitTurns = options?.EmitTurns ?? false;

        var thinking = new StringBuilder();
        var content = new StringBuilder();
        int? tokenCount = null;
        var toolCalls = ImmutableArray.CreateBuilder<ToolCall>();
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
                        await PublishModelFragment(ModelRole.Thought, new AgentThoughtFragment(thinking.ToString(), ChannelId: channelId, Final: false), sessionId, correlationId, cancellationToken);
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
                        await PublishModelFragment(ModelRole.Assistant, new AgentMessageFragment(content.ToString(), ChannelId: channelId, Final: false), sessionId, correlationId, cancellationToken);
                        break;
                    case IModelToolCallFragment toolFragment:
                        LogToolCall(toolFragment.Call.Source, toolFragment.Call.Name, toolFragment.Call.CallId, toolFragment.Call.ArgumentsJson);
                        toolCalls.Add(toolFragment.Call);
                        var agentToolCallFragment = new AgentToolCallFragment(
                                toolFragment.Call.Source,
                                toolFragment.Call.Name,
                                toolFragment.Call.ArgumentsJson,
                                toolFragment.Call.CallId);
                        await PublishModelFragment(ModelRole.Tool, agentToolCallFragment, sessionId, correlationId, cancellationToken);
                        break;
                    case IModelCompletionResponse completion:
                        tokenCount = completion.TokenCount;
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            interrupted = true;
            cancellationToken = errorStateCancellationToken.Token;
            errorStateCancellationToken.CancelAfter(_interruptFinalizeTimeout);
        }

        if (thinking.Length > 0)
        {
            await PublishModelFragment(ModelRole.Thought, new AgentThoughtFragment(thinking.ToString(), ChannelId: channelId, Final: true), sessionId, correlationId, cancellationToken);
            if (emitTurns)
            {
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
            await PublishModelFragment(ModelRole.Assistant, new AgentMessageFragment(finalContent, ChannelId: channelId, Final: true), sessionId, correlationId, cancellationToken);
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
            Interrupted: interrupted,
            Suppressed: suppressed);
    }

    private async Task PublishModelFragment<T>(
        ModelRole role,
        T fragment,
        SessionId sessionId,
        Guid correlationId,
        CancellationToken cancellationToken) where T : class, IAgentMessage
    {
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
            correlationId,
            cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Tool call received: '{Source}.{Name}' (callId={CallId}) args={Arguments}")]
    private partial void LogToolCall(string source, string name, string? callId, string arguments);
}
