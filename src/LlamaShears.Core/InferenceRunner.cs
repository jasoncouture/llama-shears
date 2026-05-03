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
            if (content.Length > 0)
            {
                var assistantTurn = new ModelTurn(ModelRole.Assistant, content.ToString(), _time.GetLocalNow());
                await _eventPublisher.PublishAsync(
                    Event.WellKnown.Agent.Turn with { Id = eventId },
                    assistantTurn,
                    correlationId,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return new InferenceOutcome(thinking.ToString(), content.ToString(), tokenCount);
    }
}
