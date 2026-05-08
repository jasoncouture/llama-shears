using System.Collections.Immutable;
using System.Text;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Tools.ModelContextProtocol;

namespace LlamaShears.Core;

public sealed class InferenceRunner : IInferenceRunner
{
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private readonly IEventPublisher _eventPublisher;
    private readonly IToolCallDispatcher _toolDispatcher;
    private readonly TimeProvider _time;

    public InferenceRunner(IEventPublisher eventPublisher, IToolCallDispatcher toolDispatcher, TimeProvider time)
    {
        _eventPublisher = eventPublisher;
        _toolDispatcher = toolDispatcher;
        _time = time;
    }

    public async Task<InferenceOutcome> RunAsync(
        string eventId,
        ILanguageModel model,
        ModelPrompt prompt,
        PromptOptions? options,
        bool emitTurns,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(prompt);

        var thinking = new StringBuilder();
        var content = new StringBuilder();
        var thoughtStreamSeen = false;
        var textStreamSeen = false;
        int? tokenCount = null;
        var toolCalls = ImmutableArray.CreateBuilder<ToolCall>();

        var tools = options?.Tools ?? [];
        var dispatchTasks = new List<Task<ToolCallResult>>();
        var interrupted = false;

        try
        {
            await foreach (var fragment in model.PromptAsync(prompt, options, cancellationToken).ConfigureAwait(false))
            {
                switch (fragment)
                {
                    case IModelThoughtResponse thought:
                        thinking.Append(thought.Content);
                        thoughtStreamSeen = true;
                        await _eventPublisher.PublishAsync(
                            Event.WellKnown.Agent.Thought with { Id = eventId },
                            new AgentThoughtFragment(thinking.ToString(), Final: false),
                            correlationId,
                            cancellationToken).ConfigureAwait(false);
                        break;
                    case IModelTextResponse text:
                        content.Append(text.Content);
                        textStreamSeen = true;
                        await _eventPublisher.PublishAsync(
                            Event.WellKnown.Agent.Message with { Id = eventId },
                            new AgentMessageFragment(content.ToString(), Final: false),
                            correlationId,
                            cancellationToken).ConfigureAwait(false);
                        break;
                    case IModelToolCallFragment toolFragment:
                        toolCalls.Add(toolFragment.Call);
                        await _eventPublisher.PublishAsync(
                            Event.WellKnown.Agent.ToolCall with { Id = eventId },
                            new AgentToolCallFragment(
                                toolFragment.Call.Source,
                                toolFragment.Call.Name,
                                toolFragment.Call.ArgumentsJson,
                                toolFragment.Call.CallId),
                            correlationId,
                            cancellationToken).ConfigureAwait(false);
                        dispatchTasks.Add(_toolDispatcher.DispatchAsync(toolFragment.Call, tools, eventId, correlationId, cancellationToken).AsTask());
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
        }

        using var finalizeTokenSource = interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var finalizeToken = interrupted ? finalizeTokenSource!.Token : cancellationToken;

        if (thoughtStreamSeen)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Thought with { Id = eventId },
                new AgentThoughtFragment(thinking.ToString(), Final: true),
                correlationId,
                finalizeToken).ConfigureAwait(false);
        }
        if (textStreamSeen)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Message with { Id = eventId },
                new AgentMessageFragment(content.ToString(), Final: true),
                correlationId,
                finalizeToken).ConfigureAwait(false);
        }

        if (emitTurns)
        {
            if (thinking.Length > 0)
            {
                var thoughtTurn = new ModelTurn(ModelRole.Thought, thinking.ToString(), _time.GetLocalNow());
                await _eventPublisher.PublishAsync(
                    Event.WellKnown.Agent.Turn with { Id = eventId },
                    thoughtTurn,
                    correlationId,
                    finalizeToken).ConfigureAwait(false);
            }
            if (content.Length > 0 || toolCalls.Count > 0)
            {
                var assistantTurn = new ModelTurn(ModelRole.Assistant, content.ToString(), _time.GetLocalNow())
                {
                    ToolCalls = toolCalls.ToImmutable(),
                };
                await _eventPublisher.PublishAsync(
                    Event.WellKnown.Agent.Turn with { Id = eventId },
                    assistantTurn,
                    correlationId,
                    finalizeToken).ConfigureAwait(false);
            }
        }


        return new InferenceOutcome(
            thinking.ToString(),
            content.ToString(),
            tokenCount,
            toolCalls.ToImmutable(),
            [.. await Task.WhenAll(dispatchTasks).ConfigureAwait(false)],
            Interrupted: interrupted);
    }
}
