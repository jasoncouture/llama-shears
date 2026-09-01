using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Pipeline;

public sealed class EphemeralContextMiddleware : IAgentMiddleware
{
    private const string DefaultChannel = "default";

    private readonly IPromptContextProvider _promptContext;
    private readonly IMemorySearcher _memorySearcher;
    private readonly IAgentStateTracker _stateTracker;
    private readonly IDataContextScope _dataScope;
    private readonly TimeProvider _time;

    public EphemeralContextMiddleware(
        IPromptContextProvider promptContext,
        IMemorySearcher memorySearcher,
        IAgentStateTracker stateTracker,
        IDataContextScope dataScope,
        TimeProvider time)
    {
        _promptContext = promptContext;
        _memorySearcher = memorySearcher;
        _stateTracker = stateTracker;
        _dataScope = dataScope;
        _time = time;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.EphemeralContext;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        var config = _dataScope.GetAgentConfig();
        if (!context.Batch.IsDefaultOrEmpty)
        {
            _stateTracker.SetState(
                context.Batch[^1].ChannelId ?? DefaultChannel,
                correlationId: context.CorrelationId,
                sessionId: context.Batch[^1].SessionId);
        }

        var memories = await SearchMemoriesAsync(
            config.Id,
            GetMemorySearchQueries([.. context.AgentContext.Turns, .. context.Batch]),
            context.TurnToken);
        _dataScope.SetItem("memories", memories);
        var body = await _promptContext.GetAsync(config.PromptContext, _dataScope.Snapshot(), context.TurnToken);
        if (!string.IsNullOrWhiteSpace(body))
        {
            context.EphemeralContext = new ModelTurn(
                ModelRole.SystemEphemeral,
                body,
                _time.GetLocalNow(),
                Ephemeral: true);
        }

        await next.Invoke(context, cancellationToken);
    }

    private static IEnumerable<string> GetMemorySearchQueries(IEnumerable<ModelTurn> turns)
    {
        return turns.Reverse().Aggregate(
            new PromptSearchState(false, false, []),
            AggregateMemoryMessages,
            state => state.Turns.Select(i => i.Content));

        static PromptSearchState AggregateMemoryMessages(PromptSearchState state, ModelTurn turn)
        {
            if (state.Complete)
            {
                return state;
            }

            if (state.UserMessageSeen && turn.Role != ModelRole.User)
            {
                return state with { Complete = true };
            }

            if (turn.Role == ModelRole.User)
            {
                state = state with { UserMessageSeen = true };
            }

            return state with { Turns = [.. state.Turns, turn] };
        }
    }

    private async ValueTask<IReadOnlyList<PromptContextMemory>> SearchMemoriesAsync(
        string agentId,
        IEnumerable<string> queries,
        CancellationToken cancellationToken)
    {
        var results = new List<MemorySearchResult>();
        foreach (var query in queries)
        {
            results.AddRange(await _memorySearcher.SearchAsync(
                agentId,
                query,
                limit: null,
                minScore: null,
                cancellationToken));
        }

        return
        [
            .. results
                .Select(static i => new PromptContextMemory(i.RelativePath, i.Summary, i.Score))
                .OrderByDescending(i => i.Score)
                .DistinctBy(i => i.RelativePath)
        ];
    }
}
