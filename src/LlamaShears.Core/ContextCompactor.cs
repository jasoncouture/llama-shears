using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Common;
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
    private const int MinTurnsForCompaction = 5;

    private const int MinTokenLimitFloor = 256;

    private const int DefaultPredictDivisor = 6;
    private const int SummaryDivisor = 3;
    private const int MaxToolCallingTurns = 5;

    private const string CompactionTemplateFileName = "COMPACTION.md";
    private const string MemoryToolSource = "llamashears";
    private const string MemoryToolName = "memory_store";
    private const string KickerFileName = "PROMPT.md";
    private const string KickerSubFolder = "compaction";

    private readonly IContextStore _contextStore;
    private readonly IAgentStateTracker _stateTracker;
    private readonly IInferenceRunner _inferenceRunner;
    private readonly IEventBus _eventPublisher;
    private readonly IModelContextProtocolServerRegistry _serverRegistry;
    private readonly IModelContextProtocolToolDiscovery _toolDiscovery;
    private readonly ITemplateFileLocator _locator;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IDataContextScope _dataContextScope;
    private readonly ILogger<ContextCompactor> _logger;

    public ContextCompactor(IContextStore contextStore,
        IAgentStateTracker stateTracker,
        IInferenceRunner inferenceRunner,
        IEventBus eventPublisher,
        IModelContextProtocolServerRegistry serverRegistry,
        IModelContextProtocolToolDiscovery toolDiscovery,
        ITemplateFileLocator locator,
        ITemplateRenderer templateRenderer,
        IDataContextScope dataContextScope,
        ILogger<ContextCompactor> logger)
    {
        _contextStore = contextStore;
        _stateTracker = stateTracker;
        _inferenceRunner = inferenceRunner;
        _eventPublisher = eventPublisher;
        _serverRegistry = serverRegistry;
        _toolDiscovery = toolDiscovery;
        _locator = locator;
        _templateRenderer = templateRenderer;
        _dataContextScope = dataContextScope;
        _logger = logger;
    }

    public async ValueTask<ModelPrompt> CompactAsync(
        AgentContext agentContext,
        ModelPrompt prompt,
        bool force,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        if (prompt.Turns.Count < MinTurnsForCompaction)
        {
            return prompt;
        }

        var configuration = _dataContextScope.GetModelConfiguration();
        if (configuration.ContextLength is not { } window)
        {
            return prompt;
        }

        if (!force)
        {
            var lastReportedTokens = agentContext.LanguageModel.ContextWindowTokenCount;
            var predictBudget = ResolvePredictBudget(configuration, window);
            var nextLength = prompt.Turns[^1].Content?.Length ?? 0;
            var nextTokenEstimate = (nextLength + 1) / 2;
            var threshold = (int)Math.Floor(window * 0.75);
            if (lastReportedTokens + predictBudget + nextTokenEstimate < threshold)
            {
                return prompt;
            }
        }

        var lastTurn = prompt.Turns[^1];
        var preserveTrailingUser = lastTurn.Role is ModelRole.User or ModelRole.FrameworkUser;
        if (!preserveTrailingUser && !force)
        {
            return prompt;
        }

        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.CompactingStarted with { Id = agentContext.AgentId },
            new AgentCompactionRequest(),
            cancellationToken);
        try
        {
            var memoryTools = await ResolveMemoryStoreToolAsync(cancellationToken);
            var summary = await SummarizeAsync(
                agentContext.AgentId,
                prompt,
                window,
                preserveTrailingUser,
                memoryTools,
                cancellationToken);

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

            LogContextCompacted(agentContext.AgentId);
            await _contextStore.ClearAsync(agentContext.AgentId, archive: true, cancellationToken);
            var live = await _contextStore.OpenAsync(agentContext.AgentId, cancellationToken);
            foreach (var turn in rebuiltPrompt.Turns)
            {
                if (turn.Role == ModelRole.System)
                {
                    continue;
                }
                await live.AppendAsync(turn, cancellationToken);
            }
            return rebuiltPrompt;
        }
        finally
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.CompactingFinished with { Id = agentContext.AgentId },
                new AgentCompactionRequest(),
                CancellationToken.None);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' compacted its context to fit the window.")]
    private partial void LogContextCompacted(string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' compaction reached the tool-calling turn cap ({Limit}); stripping tools and forcing a text-only summary turn.")]
    private partial void LogCompactionToolLimitReached(string agentId, int limit);

    private async ValueTask<ImmutableArray<ToolGroup>> ResolveMemoryStoreToolAsync(CancellationToken cancellationToken)
    {
        var servers = _serverRegistry.Resolve(whitelist: [MemoryToolSource]);
        if (servers.Count == 0)
        {
            return [];
        }
        var groups = await _toolDiscovery.DiscoverAsync(servers.Keys, cancellationToken);
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

    private async ValueTask<string> LoadKickerAsync(CancellationToken cancellationToken)
    {
        var resolved = _locator.Locate(KickerSubFolder, KickerFileName, KickerFileName)
            ?? throw new FileNotFoundException(
                $"Compaction kicker template '{KickerFileName}' not found in workspace, templates, or bundled '{KickerSubFolder}/' folder.");
        var data = SnapshotScope();
        var rendered = await _templateRenderer.RenderAsync(resolved, data, cancellationToken).ConfigureAwait(false)
            ?? throw new FileNotFoundException(
                $"Compaction kicker template '{resolved}' could not be rendered.");
        return rendered;
    }

    private IReadOnlyDictionary<string, object?> SnapshotScope()
        => _dataContextScope.Snapshot();

    private async ValueTask<string> SummarizeAsync(
        string agentId,
        ModelPrompt prompt,
        int window,
        bool preserveTrailingUser,
        ImmutableArray<ToolGroup> tools,
        CancellationToken cancellationToken)
    {
        var historyCount = preserveTrailingUser
            ? prompt.Turns.Count - 1
            : prompt.Turns.Count;
        _stateTracker.SetState(channelId: "compactor", eventId: $"{agentId}-compaction");
        var historyTurns = new List<ModelTurn>(historyCount + 1);
        for (var i = 0; i < historyCount; i++)
        {
            historyTurns.Add(prompt.Turns[i]);
        }
        if (historyTurns.Count == 0 || historyTurns[^1].Role is not ModelRole.User and not ModelRole.FrameworkUser)
        {
            var kicker = await LoadKickerAsync(cancellationToken);
            historyTurns.Add(new ModelTurn(ModelRole.User, kicker, prompt.Turns[^1].Timestamp));
        }

        var summaryCap = Math.Max(window / SummaryDivisor, MinTokenLimitFloor);
        var options = new PromptOptions(
            TokenLimit: summaryCap,
            Tools: tools,
            SystemPromptTemplate: CompactionTemplateFileName);
        
        var toolCallingTurns = 0;

        while (true)
        {
            var summarizationPrompt = new ModelPrompt(historyTurns);
            var outcome = await _inferenceRunner.RunAsync(
                prompt: summarizationPrompt,
                options: options,
                cancellationToken: cancellationToken);

            if (outcome.Interrupted)
            {
                throw new CompactionFailedException("Compaction was interrupted before the model produced a summary.");
            }

            if (!outcome.ToolCalls.IsDefaultOrEmpty)
            {
                toolCallingTurns++;
                var assistantTurn = new ModelTurn(ModelRole.Assistant, outcome.Content, prompt.Turns[^1].Timestamp)
                {
                    ToolCalls = outcome.ToolCalls,
                };
                historyTurns.Add(assistantTurn);
                for (var i = 0; i < outcome.ToolCalls.Length; i++)
                {
                    historyTurns.Add(new ModelTurn(ModelRole.Tool, outcome.ToolResults[i].Content, prompt.Turns[^1].Timestamp)
                    {
                        ToolCall = outcome.ToolCalls[i],
                        IsError = outcome.ToolResults[i].IsError,
                    });
                }
                if (toolCallingTurns >= MaxToolCallingTurns)
                {
                    LogCompactionToolLimitReached(agentId, MaxToolCallingTurns);
                    options = options with { Tools = [] };
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

    private static int ResolvePredictBudget(ModelConfiguration configuration, int window)
    {
        if (configuration.TokenLimit > 0)
        {
            return configuration.TokenLimit;
        }
        return Math.Max(window / DefaultPredictDivisor, MinTokenLimitFloor);
    }
}
