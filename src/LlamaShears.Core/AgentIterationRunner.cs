using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentIterationRunner : IAgentIterationRunner
{
    private const string DefaultChannel = "default";
    private const int EmptyResponseRetryLimit = 3;
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<AgentIterationRunner> _logger;
    private readonly TimeProvider _time;
    private readonly IEventPublisher _eventPublisher;
    private readonly IDataContextScope _dataScope;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAgentContextProvider _agentContextProvider;

    public AgentIterationRunner(
        ILogger<AgentIterationRunner> logger,
        TimeProvider time,
        IEventPublisher eventPublisher,
        IDataContextScope dataScope,
        IServiceScopeFactory scopeFactory,
        IAgentContextProvider agentContextProvider)
    {
        _logger = logger;
        _time = time;
        _eventPublisher = eventPublisher;
        _dataScope = dataScope;
        _scopeFactory = scopeFactory;
        _agentContextProvider = agentContextProvider;
    }

    public async Task<IterationOutcome> RunAsync(
        IAgentContext context,
        ImmutableArray<ModelTurn> batch,
        Guid correlationId,
        CancellationToken outerCancellationToken,
        CancellationToken turnCancellationToken)
    {
        await using var bundle = _scopeFactory.CreateAsyncScopeWithData();
        await bundle.ServiceScope.ApplyScopeDataAsync(turnCancellationToken);
        bundle.ServiceProvider.GetRequiredService<IAgentStateTracker>()
            .SetState(batch[^1].ChannelId ?? DefaultChannel, correlationId: correlationId);
        var agentId = _dataScope.GetAgentConfig().Id;

        foreach (var turn in batch)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = agentId },
                turn,
                correlationId,
                outerCancellationToken);
        }

        var prompt = new ModelPrompt([.. context.Turns]);
        var agentContextSnapshot =
            await _agentContextProvider.CreateAgentContextAsync(agentId, turnCancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{agentId}'.");
        var inferenceRunner = bundle.ServiceProvider.GetRequiredService<IInferenceRunner>();
        var serverRegistry = bundle.ServiceProvider.GetRequiredService<IModelContextProtocolServerRegistry>();
        var toolDiscovery = bundle.ServiceProvider.GetRequiredService<IModelContextProtocolToolDiscovery>();
        var compactor = bundle.ServiceProvider.GetRequiredService<IContextCompactor>();

        prompt = await compactor
            .CompactAsync(agentContextSnapshot, prompt, force: false, turnCancellationToken);
        var servers = serverRegistry.Resolve(_dataScope.GetAgentConfig().ModelContextProtocolServers);
        var tools = await toolDiscovery.DiscoverAsync(servers.Keys, turnCancellationToken);
        var systemPromptTemplate = _dataScope.GetAgentConfig().SystemPrompt;
        var promptOptions = systemPromptTemplate is null
            ? new PromptOptions(Tools: tools, InjectEphemeralContext: true, EmitTurns: true)
            : new PromptOptions(Tools: tools, InjectEphemeralContext: true, EmitTurns: true, SystemPromptTemplate: systemPromptTemplate);

        InferenceOutcome outcome;
        var emptyAttempt = 0;

        while (true)
        {
            outcome = await inferenceRunner.RunAsync(
                prompt: prompt,
                options: promptOptions,
                cancellationToken: turnCancellationToken);
            if (outcome.Interrupted)
            {
                break;
            }
            if (outcome.Suppressed)
            {
                break;
            }
            if (!outcome.ToolCalls.IsDefaultOrEmpty || outcome.Content.Length > 0)
            {
                break;
            }
            emptyAttempt++;
            if (emptyAttempt > EmptyResponseRetryLimit)
            {
                LogEmptyResponseGaveUp(agentId, emptyAttempt);
                break;
            }
            LogEmptyResponseRetrying(agentId, emptyAttempt);
            prompt = prompt with
            {
                Turns =
                [
                    .. prompt.Turns,
                    new ModelTurn(ModelRole.User,
                        "<SYSTEM>ERROR: You must reply with content, or a tool. Please try again. If you do not wish to respond, please reply with exactly: NO_RESPONSE</SYSTEM>",
                        _time.GetLocalNow(), prompt.Turns[^1].ChannelId)
                ]
            };
        }

        using var interruptedTokenSource =
            outcome.Interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var publishToken = outcome.Interrupted ? interruptedTokenSource!.Token : turnCancellationToken;

        if (outcome.TokenCount is { } tokens)
        {
            await context.AppendAsync(new ModelTokenInformationContextEntry(tokens, _time.GetLocalNow()), publishToken);
        }

        if (outcome.Interrupted)
        {
            return new IterationOutcome(Interrupted: true, ToolResultTurns: []);
        }

        if (outcome.ToolCalls.IsDefaultOrEmpty)
        {
            return new IterationOutcome(Interrupted: false, ToolResultTurns: []);
        }

        var toolTurns = ImmutableArray.CreateBuilder<ModelTurn>(outcome.ToolCalls.Length);
        for (var i = 0; i < outcome.ToolCalls.Length; i++)
        {
            toolTurns.Add(new ModelTurn(
                ModelRole.Tool,
                outcome.ToolResults[i].Content,
                _time.GetLocalNow())
            {
                ToolCall = outcome.ToolCalls[i],
                IsError = outcome.ToolResults[i].IsError,
            });
        }
        return new IterationOutcome(Interrupted: false, ToolResultTurns: toolTurns.ToImmutable());
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Agent '{AgentId}' received an empty response from the model; retrying without committing the turn (attempt {Attempt}).")]
    private partial void LogEmptyResponseRetrying(string agentId, int attempt);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Agent '{AgentId}' received {Attempts} consecutive empty responses from the model; giving up on this turn.")]
    private partial void LogEmptyResponseGaveUp(string agentId, int attempts);
}
