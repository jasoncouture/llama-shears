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
    private const string CompactionTemplateFileName = "COMPACTION.md";
    private const int SummarizerMaxTurns = 5;

    private readonly IContextCompactor _compactor;
    private readonly ISubagentRunner _subagentRunner;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IEventBus _eventPublisher;
    private readonly IDataContextScope _dataScope;

    public CompactionMiddleware(
        IContextCompactor compactor,
        ISubagentRunner subagentRunner,
        IAgentContextProvider agentContextProvider,
        IEventBus eventPublisher,
        IDataContextScope dataScope)
    {
        _compactor = compactor;
        _subagentRunner = subagentRunner;
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
        try
        {
            var summary = await SummarizeAsync(plan, context.TurnToken);
            context.Prompt = await _compactor.CommitAsync(plan, summary, context.TurnToken);
            if (context.CompactionOnly)
            {
                return;
            }

            await next.Invoke(context, cancellationToken);
        }
        finally
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.CompactingFinished with { Id = currentSession },
                new AgentCompactionRequest(),
                CancellationToken.None);
        }
    }

    private async Task<string> SummarizeAsync(CompactionPlan plan, CancellationToken cancellationToken)
    {
        var result = await _subagentRunner.RunAsync(
            new SubagentRunRequest(
                Prompt: plan.Transcript.Content,
                MaxTurns: SummarizerMaxTurns,
                AwaitResult: true,
                SystemPrompt: CompactionTemplateFileName,
                PromptContext: CompactionTemplateFileName),
            cancellationToken);
        if (!result.Ok)
        {
            throw new CompactionFailedException(
                result.Error ?? "Compaction summarizer failed before producing a summary.");
        }

        var summary = result.Output?.Trim() ?? "";
        if (summary.Length == 0)
        {
            throw new CompactionFailedException(
                "Model produced an empty summary; cannot compact context.");
        }

        return summary;
    }
}
