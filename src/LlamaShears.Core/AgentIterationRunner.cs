using System.Collections.Immutable;
using LlamaShears.Core.Abstractions;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Common;
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
    private readonly IDataContextScope _dataScope;
    private readonly IServiceScopeFactory _scopeFactory;

    public AgentIterationRunner(
        ILogger<AgentIterationRunner> logger,
        TimeProvider time,
        IDataContextScope dataScope,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _time = time;
        _dataScope = dataScope;
        _scopeFactory = scopeFactory;
    }

    public async Task<IterationOutcome> RunAsync(AgentPipelineContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var batch = context.Batch;
        var correlationId = context.CorrelationId;
        var turnCancellationToken = context.TurnToken;
        var agentContext = context.AgentContext;
        var prompt = context.Prompt
            ?? throw new InvalidOperationException(
                "Compaction middleware must set AgentPipelineContext.Prompt before the iteration runs.");
        var session = context.SessionId
            ?? throw new InvalidOperationException(
                "Run-iteration middleware must set AgentPipelineContext.SessionId before the iteration runs.");

        await using var bundle = _scopeFactory.CreateAsyncScopeWithData();
        bundle.ServiceScope.ApplyScopeData(turnCancellationToken);
        var sessionId = batch[^1].SessionId;
        bundle.ServiceProvider.GetRequiredService<IAgentStateTracker>()
            .SetState(batch[^1].ChannelId ?? DefaultChannel, correlationId: correlationId, sessionId: sessionId);
        var agentId = _dataScope.GetAgentConfig().Id;
        var inferenceRunner = bundle.ServiceProvider.GetRequiredService<IInferenceRunner>();
        var serverRegistry = bundle.ServiceProvider.GetRequiredService<IModelContextProtocolServerRegistry>();
        var toolDiscovery = bundle.ServiceProvider.GetRequiredService<IModelContextProtocolToolDiscovery>();
        var servers = serverRegistry.Resolve(_dataScope.GetAgentConfig().ModelContextProtocolServers);
        var tools = await toolDiscovery.DiscoverAsync(servers.Keys, turnCancellationToken);
        var promptOptions = new PromptOptions(
            Tools: tools,
            EmitTurns: true);

        InferenceOutcome outcome;
        var emptyAttempt = 0;

        while (true)
        {
            outcome = await inferenceRunner.RunAsync(
                prompt: prompt,
                options: promptOptions,
                sessionId: session,
                correlationId: correlationId,
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
                        $"<SYSTEM>ERROR: You must reply with content, or a tool. Please try again. If you do not wish to respond, please reply with exactly: {Sentinel.NoResponse}</SYSTEM>",
                        _time.GetLocalNow(), prompt.Turns[^1].ChannelId)
                ]
            };
        }

        using var interruptedTokenSource =
            outcome.Interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var publishToken = outcome.Interrupted ? interruptedTokenSource!.Token : turnCancellationToken;

        if (outcome.TokenCount is { } tokens)
        {
            await agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens, _time.GetLocalNow()), publishToken);
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
                SessionId = sessionId,
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
