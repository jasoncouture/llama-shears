using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class ContextCompactor : IContextCompactor
{
    // 1 system + 2 user + 2 assistant. Below this the trade-off (one model
    // call to produce a summary that wouldn't have been near the limit
    // anyway) isn't worth it; also avoids the pathological case where the
    // summary itself triggers the next compaction.
    private const int MinTurnsForCompaction = 5;

    // Floor for any predict-budget calculation. The compactor never
    // assumes there's less than this many tokens of headroom on either
    // the agent's next response or the summary it requests.
    private const int MinTokenLimitFloor = 256;

    // Default fraction of the context window reserved for the model's
    // response when ModelConfiguration.TokenLimit is unset. Must match
    // the provider's own default reasoning so the budget check stays
    // honest; widen the divisor for larger windows in a follow-up.
    private const int DefaultPredictDivisor = 6;

    // Cap on the summarization call's response. Bounds the rebuilt
    // context so a runaway summary can't itself blow the window.
    private const int SummaryDivisor = 3;

    private const string CompactionTemplateName = "COMPACTION";
    private const string MemoryToolSource = "llamashears";
    private const string MemoryToolName = "memory_store";
    private const string SummarizeKicker = "Save important memories with memory_store, then produce the summary.";

    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IContextStore _contextStore;
    private readonly IInferenceRunner _inferenceRunner;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly IModelContextProtocolServerRegistry _serverRegistry;
    private readonly IModelContextProtocolToolDiscovery _toolDiscovery;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly ILogger<ContextCompactor> _logger;

    public ContextCompactor(
        IAgentContextProvider agentContextProvider,
        IContextStore contextStore,
        IInferenceRunner inferenceRunner,
        IEventPublisher eventPublisher,
        ISystemPromptProvider systemPrompt,
        IModelContextProtocolServerRegistry serverRegistry,
        IModelContextProtocolToolDiscovery toolDiscovery,
        ICurrentAgentAccessor currentAgent,
        ILogger<ContextCompactor> logger)
    {
        _agentContextProvider = agentContextProvider;
        _contextStore = contextStore;
        _inferenceRunner = inferenceRunner;
        _eventPublisher = eventPublisher;
        _systemPrompt = systemPrompt;
        _serverRegistry = serverRegistry;
        _toolDiscovery = toolDiscovery;
        _currentAgent = currentAgent;
        _logger = logger;
    }

    public async ValueTask<ModelPrompt> CompactAsync(
        AgentContext agentContext,
        ModelPrompt prompt,
        ILanguageModel model,
        ModelConfiguration configuration,
        bool force,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(configuration);

        if (prompt.Turns.Count < MinTurnsForCompaction)
        {
            return prompt;
        }

        if (configuration.ContextLength is not { } window)
        {
            return prompt;
        }

        if (!force)
        {
            var totalEstimate = agentContext.LanguageModel.ContextWindowTokenCount;
            var predictBudget = ResolvePredictBudget(configuration, window);
            if (totalEstimate + predictBudget < window)
            {
                return prompt;
            }
        }

        var lastTurn = prompt.Turns[^1];
        var preserveTrailingUser = lastTurn.Role is ModelRole.User or ModelRole.FrameworkUser;
        if (!preserveTrailingUser && !force)
        {
            // Auto-compaction's rebuild assumes a user-anchored prompt
            // (the just-arrived message is preserved while everything
            // before it is summarized). User-forced /compact takes the
            // force path and skips this requirement.
            return prompt;
        }

        // Surface the start/finish of compaction so the UI can show a
        // busy indicator while the model produces the summary. Finish
        // fires in finally so a thrown summarization doesn't strand the
        // UI in a permanent compacting state.
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.CompactingStarted with { Id = agentContext.AgentId },
            new AgentCompactionMarker(),
            cancellationToken).ConfigureAwait(false);
        try
        {
            var agentInfo = new AgentInfo(
                AgentId: agentContext.AgentId,
                ModelId: configuration.ModelId,
                ContextWindowSize: configuration.ContextLength ?? 0);
            using var scope = _currentAgent.BeginScope(agentInfo);
            var memoryTools = await ResolveMemoryStoreToolAsync(cancellationToken).ConfigureAwait(false);
            var summary = await SummarizeAsync(
                agentContext.AgentId,
                prompt,
                model,
                window,
                preserveTrailingUser,
                memoryTools,
                cancellationToken).ConfigureAwait(false);

            var rebuilt = new List<ModelTurn>(3);
            if (prompt.Turns[0].Role == ModelRole.System)
            {
                rebuilt.Add(prompt.Turns[0]);
            }
            rebuilt.Add(new ModelTurn(ModelRole.Assistant, summary, lastTurn.Timestamp));
            if (preserveTrailingUser)
            {
                rebuilt.Add(lastTurn);
            }
            var rebuiltPrompt = new ModelPrompt(rebuilt);

            // Compaction succeeded: archive the old persisted context and
            // re-seed it with the rebuilt non-system turns. The system turn
            // is reconstructed per-call by the caller and never persisted.
            LogContextCompacted(_logger, agentContext.AgentId);
            await _contextStore.ClearAsync(agentContext.AgentId, archive: true, cancellationToken).ConfigureAwait(false);
            var live = await _contextStore.OpenAsync(agentContext.AgentId, cancellationToken).ConfigureAwait(false);
            foreach (var turn in rebuiltPrompt.Turns)
            {
                if (turn.Role == ModelRole.System)
                {
                    continue;
                }
                await live.AppendAsync(turn, cancellationToken).ConfigureAwait(false);
            }
            return rebuiltPrompt;
        }
        finally
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.CompactingFinished with { Id = agentContext.AgentId },
                new AgentCompactionMarker(),
                CancellationToken.None).ConfigureAwait(false);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' compacted its context to fit the window.")]
    private static partial void LogContextCompacted(ILogger logger, string agentId);

    private async ValueTask<ImmutableArray<ToolGroup>> ResolveMemoryStoreToolAsync(CancellationToken cancellationToken)
    {
        var servers = _serverRegistry.Resolve(whitelist: [MemoryToolSource]);
        if (servers.Count == 0)
        {
            return [];
        }
        var groups = await _toolDiscovery.DiscoverAsync(servers, cancellationToken).ConfigureAwait(false);
        foreach (var group in groups)
        {
            if (!string.Equals(group.Source, MemoryToolSource, StringComparison.Ordinal))
            {
                continue;
            }
            foreach (var descriptor in group.Tools)
            {
                if (string.Equals(descriptor.Name, MemoryToolName, StringComparison.Ordinal))
                {
                    return [new ToolGroup(MemoryToolSource, [descriptor])];
                }
            }
        }
        return [];
    }

    private async ValueTask<string> SummarizeAsync(
        string agentId,
        ModelPrompt prompt,
        ILanguageModel model,
        int window,
        bool preserveTrailingUser,
        ImmutableArray<ToolGroup> tools,
        CancellationToken cancellationToken)
    {
        // Auto-compaction excludes the trailing user message (it gets
        // re-attached to the rebuilt prompt). User-forced compaction
        // has no trailing user to preserve, so all turns go into the
        // summary input. Either way the call ends with a fresh
        // user-role instruction asking for the summary.
        var historyCount = preserveTrailingUser
            ? prompt.Turns.Count - 1
            : prompt.Turns.Count;
        var systemBody = await _systemPrompt.GetAsync(
            CompactionTemplateName,
            new SystemPromptTemplateParameters(AgentId: agentId),
            cancellationToken).ConfigureAwait(false);
        var historyTurns = new List<ModelTurn>(historyCount + 2);
        var compactionInserted = false;
        for (var i = 0; i < historyCount; i++)
        {
            var turn = prompt.Turns[i];
            historyTurns.Add(turn);
            if (turn.Role == ModelRole.System && !compactionInserted)
            {
                historyTurns.Add(new ModelTurn(ModelRole.System, systemBody, turn.Timestamp));
                compactionInserted = true;
            }
        }
        if (!compactionInserted)
        {
            historyTurns.Insert(0, new ModelTurn(ModelRole.System, systemBody, prompt.Turns[^1].Timestamp));
        }
        if (historyTurns[^1].Role is not ModelRole.User and not ModelRole.FrameworkUser)
        {
            historyTurns.Add(new ModelTurn(ModelRole.User, SummarizeKicker, prompt.Turns[^1].Timestamp));
        }

        var summarizationPrompt = new ModelPrompt(historyTurns);
        var summaryCap = Math.Max(window / SummaryDivisor, MinTokenLimitFloor);
        var options = new PromptOptions(TokenLimit: summaryCap, Tools: tools);

        var outcome = await _inferenceRunner.RunAsync(
            eventId: $"{agentId}-compaction",
            model: model,
            prompt: summarizationPrompt,
            options: options,
            emitTurns: false,
            correlationId: Guid.CreateVersion7(),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var summary = outcome.Content.Trim();
        if (summary.Length == 0)
        {
            throw new CompactionFailedException(
                "Model produced an empty summary; cannot compact context.");
        }
        return summary;
    }

    private static int ResolvePredictBudget(ModelConfiguration configuration, int window)
    {
        if (configuration.TokenLimit > 0)
        {
            return configuration.TokenLimit;
        }
        return Math.Max(window / DefaultPredictDivisor, MinTokenLimitFloor);
    }
}
