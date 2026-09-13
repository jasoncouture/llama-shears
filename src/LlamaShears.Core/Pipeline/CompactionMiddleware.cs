using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Pipeline;

public sealed class CompactionMiddleware : IAgentMiddleware
{
    private const int MaxToolCallingTurns = 5;

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
        var livePrompt = new ModelPrompt([.. context.AgentContext.Turns]);

        var plan = await _compactor.TryPrepareAsync(
            snapshot,
            livePrompt,
            context.ForceCompaction,
            context.TurnToken);
        if (plan is null)
        {
            if (context.CompactionOnly)
            {
                return;
            }

            context.Prompt = livePrompt;
            await next.Invoke(context, cancellationToken);
            return;
        }

        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.CompactingStarted with { Id = currentSession },
            new AgentCompactionRequest(),
            cancellationToken);
        var previousConfig = _dataScope.GetAgentConfig();
        var originalBatch = context.Batch;
        try
        {
            var summary = await RunSummarizerPassAsync(context, plan, previousConfig, next, cancellationToken);
            context.Prompt = await _compactor.CommitAsync(plan, summary, context.TurnToken);
            _dataScope.SetItem(AgentConfig.DataKey, previousConfig);
            if (context.CompactionOnly)
            {
                return;
            }

            context.FrameworkPass = false;
            context.Outcome = null;
            context.EphemeralContext = null;
            context.Batch = originalBatch;
            context.SystemPrompt = null;
            await next.Invoke(context, cancellationToken);
        }
        finally
        {
            _dataScope.SetItem(AgentConfig.DataKey, previousConfig);
            context.FrameworkPass = false;
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.CompactingFinished with { Id = currentSession },
                new AgentCompactionRequest(),
                CancellationToken.None);
        }
    }

    private async Task<string> RunSummarizerPassAsync(
        AgentPipelineContext context,
        CompactionPlan plan,
        AgentConfig previousConfig,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        _dataScope.SetItem(
            AgentConfig.DataKey,
            PromptedAgentStartInformation.CreateDefaultSubAgentConfig("compaction", previousConfig));

        context.FrameworkPass = true;
        context.Batch = [plan.Transcript];
        var history = new List<ModelTurn> { plan.Transcript };
        var toolCallingTurns = 0;

        while (true)
        {
            context.Prompt = new ModelPrompt(history);
            context.Outcome = null;
            context.EphemeralContext = null;
            context.SystemPrompt = null;
            await next.Invoke(context, cancellationToken);

            var outcome = context.Outcome
                ?? throw new CompactionFailedException("Summarizer pass returned no iteration outcome.");
            if (outcome.Interrupted)
            {
                throw new CompactionFailedException("Compaction was interrupted before the model produced a summary.");
            }

            if (!outcome.ToolCalls.IsDefaultOrEmpty)
            {
                toolCallingTurns++;
                history.Add(new ModelTurn(ModelRole.Assistant, outcome.Content, plan.Transcript.Timestamp)
                {
                    ToolCalls = outcome.ToolCalls,
                });
                history.AddRange(outcome.ToolResultTurns);
                context.Batch = [.. outcome.ToolResultTurns];
                if (toolCallingTurns >= MaxToolCallingTurns)
                {
                    var summaryAfterCap = outcome.Content.Trim();
                    if (summaryAfterCap.Length > 0 && outcome.ToolResultTurns.IsDefaultOrEmpty)
                    {
                        return summaryAfterCap;
                    }
                }
                continue;
            }

            var summary = outcome.Content.Trim();
            if (summary.Length == 0)
            {
                throw new CompactionFailedException(
                    "Model produced an empty summary; cannot compact context.");
            }
            return summary;
        }
    }
}
