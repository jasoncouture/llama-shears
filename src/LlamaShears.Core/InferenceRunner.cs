using System.Collections.Immutable;
using System.Text;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core;

public sealed class InferenceRunner : IInferenceRunner
{
    private readonly IEventPublisher _eventPublisher;
    private readonly TimeProvider _time;

    public InferenceRunner(IEventPublisher eventPublisher, TimeProvider time)
    {
        _eventPublisher = eventPublisher;
        _time = time;
    }

    public async Task<InferenceOutcome> RunAsync(
        string eventId,
        ILanguageModel model,
        ModelPrompt prompt,
        PromptOptions? options,
        bool emitTurns,
        Guid correlationId,
        Func<ToolCall, CancellationToken, ValueTask<ToolCallResult>>? dispatchTool,
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
        // Dispatch tasks accumulate as tool calls arrive. With a non-null
        // dispatcher the runner kicks off each tool the moment its
        // fragment lands, so execution overlaps with the model's
        // continued streaming of subsequent calls. With a null dispatcher
        // (e.g. compaction) the list stays empty and ToolResults on the
        // outcome is default.
        var dispatchTasks = dispatchTool is null ? null : new List<Task<ToolCallResult>>();

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
                    if (dispatchTool is not null)
                    {
                        var call = toolFragment.Call;
                        dispatchTasks!.Add(Task.Run(() => dispatchTool.Invoke(call, cancellationToken).AsTask(), cancellationToken));
                    }
                    break;
                case IModelCompletionResponse completion:
                    tokenCount = completion.TokenCount;
                    break;
            }
        }

        if (thoughtStreamSeen)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Thought with { Id = eventId },
                new AgentThoughtFragment(thinking.ToString(), Final: true),
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }
        if (textStreamSeen)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Message with { Id = eventId },
                new AgentMessageFragment(content.ToString(), Final: true),
                correlationId,
                cancellationToken).ConfigureAwait(false);
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
                    cancellationToken).ConfigureAwait(false);
            }
            // Emit an assistant turn whenever there is anything to remember:
            // textual content, tool calls, or both. A pure tool-call response
            // has empty content but still belongs in the conversation history
            // so the model can correlate the eventual tool result with its
            // own request.
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
                    cancellationToken).ConfigureAwait(false);
            }
        }

        var toolResults = ImmutableArray<ToolCallResult>.Empty;
        if (dispatchTasks is { Count: > 0 })
        {
            var results = await Task.WhenAll(dispatchTasks).ConfigureAwait(false);
            toolResults = ImmutableArray.Create(results);
        }

        return new InferenceOutcome(
            thinking.ToString(),
            content.ToString(),
            tokenCount,
            toolCalls.ToImmutable(),
            toolResults);
    }
}
