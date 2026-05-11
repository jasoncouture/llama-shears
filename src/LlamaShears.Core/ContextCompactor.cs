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

    private const string CompactionTemplateFileName = "COMPACTION.md";
    private const string MemoryToolSource = "llamashears";
    private const string MemoryToolName = "memory_store";
    private const string KickerFileName = "PROMPT.md";
    private const string KickerSubFolder = "compaction";

    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IContextStore _contextStore;
    private readonly IInferenceRunner _inferenceRunner;
    private readonly IEventPublisher _eventPublisher;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly IModelContextProtocolServerRegistry _serverRegistry;
    private readonly IModelContextProtocolToolDiscovery _toolDiscovery;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly ITemplateFileLocator _locator;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly IDataContextFactory _dataContextFactory;
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
        ITemplateFileLocator locator,
        ITemplateRenderer templateRenderer,
        IDataContextFactory dataContextFactory,
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
        _locator = locator;
        _templateRenderer = templateRenderer;
        _dataContextFactory = dataContextFactory;
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
            new AgentCompactionMarker(),
            cancellationToken);
        try
        {
            var agentInfo = new AgentInfo(
                AgentId: agentContext.AgentId,
                ModelId: configuration.Id,
                ContextWindowSize: configuration.ContextLength ?? 0);
            using var scope = _currentAgent.BeginScope(agentInfo);
            var memoryTools = await ResolveMemoryStoreToolAsync(cancellationToken);
            var summary = await SummarizeAsync(
                agentContext.AgentId,
                prompt,
                model,
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
                new AgentCompactionMarker(),
                CancellationToken.None);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' compacted its context to fit the window.")]
    private partial void LogContextCompacted(string agentId);

    private async ValueTask<ImmutableArray<ToolGroup>> ResolveMemoryStoreToolAsync(CancellationToken cancellationToken)
    {
        var servers = _serverRegistry.Resolve(whitelist: [MemoryToolSource]);
        if (servers.Count == 0)
        {
            return [];
        }
        var groups = await _toolDiscovery.DiscoverAsync(servers, cancellationToken);
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
        => _dataContextFactory.Current?.Snapshot()
            ?? ImmutableDictionary.Create<string, object?>(StringComparer.OrdinalIgnoreCase);

    private async ValueTask<string> SummarizeAsync(
        string agentId,
        ModelPrompt prompt,
        ILanguageModel model,
        int window,
        bool preserveTrailingUser,
        ImmutableArray<ToolGroup> tools,
        CancellationToken cancellationToken)
    {
        var historyCount = preserveTrailingUser
            ? prompt.Turns.Count - 1
            : prompt.Turns.Count;
        var systemBody = await _systemPrompt.GetAsync(
            CompactionTemplateFileName,
            SnapshotScope(),
            cancellationToken);
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
            var kicker = await LoadKickerAsync(cancellationToken);
            historyTurns.Add(new ModelTurn(ModelRole.User, kicker, prompt.Turns[^1].Timestamp));
        }

        var summaryCap = Math.Max(window / SummaryDivisor, MinTokenLimitFloor);
        var options = new PromptOptions(TokenLimit: summaryCap, Tools: tools);
        var correlationId = Guid.CreateVersion7();

        while (true)
        {
            var summarizationPrompt = new ModelPrompt(historyTurns);
            var outcome = await _inferenceRunner.RunAsync(
                eventId: $"{agentId}-compaction",
                model: model,
                prompt: summarizationPrompt,
                options: options,
                emitTurns: false,
                correlationId: correlationId,
                cancellationToken: cancellationToken);

            if (outcome.Interrupted)
            {
                throw new CompactionFailedException("Compaction was interrupted before the model produced a summary.");
            }

            if (!outcome.ToolCalls.IsDefaultOrEmpty)
            {
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
