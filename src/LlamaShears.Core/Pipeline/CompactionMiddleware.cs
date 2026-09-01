using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Pipeline;

public sealed class CompactionMiddleware : IAgentMiddleware
{
    private readonly IContextCompactor _compactor;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IEventBus _eventPublisher;
    private readonly IDataContextScope _dataScope;

    public CompactionMiddleware(
        IContextCompactor compactor,
        IAgentContextProvider agentContextProvider,
        IEventBus eventPublisher,
        IDataContextScope dataScope)
    {
        _compactor = compactor;
        _agentContextProvider = agentContextProvider;
        _eventPublisher = eventPublisher;
        _dataScope = dataScope;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.Compaction;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        var currentSession = _dataScope.GetCurrentSessionId();
        foreach (var turn in context.Batch)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = currentSession },
                turn,
                context.CorrelationId,
                context.ShutdownToken);
        }

        var agentId = _dataScope.GetAgentConfig().Id;
        var snapshot = await _agentContextProvider
            .CreateAgentContextAsync(currentSession, context.TurnToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Agent context provider returned null for running agent '{agentId}'.");
        var prompt = context.SystemPrompt is { } systemPrompt
            ? new ModelPrompt([systemPrompt, .. context.AgentContext.Turns])
            : new ModelPrompt([.. context.AgentContext.Turns]);
        context.Prompt = await _compactor.CompactAsync(snapshot, prompt, force: false, context.TurnToken);
        await next.Invoke(context, cancellationToken);
    }
}
