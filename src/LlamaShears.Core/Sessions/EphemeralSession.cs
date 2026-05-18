using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Sessions;

public sealed partial class EphemeralSession : IEphemeralSession
{
    internal const int DefaultMaxIterations = 8;

    private readonly IAsyncDisposable _scope;
    private readonly IAgentContext _agentContext;
    private readonly IAgentIterationRunner _iterationRunner;
    private readonly IEventPublisher _eventPublisher;
    private readonly EphemeralSessionContext _sessionContext;
    private readonly TimeProvider _time;
    private readonly ILogger<EphemeralSession> _logger;
    private readonly int _maxIterations;
    private readonly EphemeralSessionReference _reference;
    private int _disposed;

    public EphemeralSession(
        IAsyncDisposable scope,
        IAgentContext agentContext,
        IAgentIterationRunner iterationRunner,
        IEventPublisher eventPublisher,
        EphemeralSessionContext sessionContext,
        TimeProvider time,
        ILogger<EphemeralSession> logger,
        int maxIterations)
    {
        _scope = scope;
        _agentContext = agentContext;
        _iterationRunner = iterationRunner;
        _eventPublisher = eventPublisher;
        _sessionContext = sessionContext;
        _time = time;
        _logger = logger;
        _maxIterations = maxIterations;
        _reference = new EphemeralSessionReference(sessionContext.Parent.AgentId, sessionContext.SessionId);
    }

    public EphemeralSessionReference Reference => _reference;

    public async Task<EphemeralRunResult> RunAsync(string initialPrompt, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(initialPrompt);

        var pending = new Queue<ModelTurn>();
        pending.Enqueue(new ModelTurn(
            ModelRole.User,
            initialPrompt,
            _time.GetLocalNow(),
            ChannelId: _sessionContext.ChannelId)
        {
            SessionId = _sessionContext.SessionId,
        });

        var iterations = 0;
        var interrupted = false;
        while (pending.Count > 0 && !cancellationToken.IsCancellationRequested)
        {
            if (iterations >= _maxIterations)
            {
                LogIterationCapHit(_reference.AgentId, _sessionContext.SessionId, iterations);
                break;
            }

            var batchBuilder = ImmutableArray.CreateBuilder<ModelTurn>(pending.Count);
            while (pending.Count > 0)
            {
                batchBuilder.Add(pending.Dequeue());
            }

            var correlationId = Guid.CreateVersion7();
            var outcome = await _iterationRunner.RunAsync(
                _agentContext,
                batchBuilder.ToImmutable(),
                correlationId,
                cancellationToken,
                cancellationToken);

            iterations++;
            if (outcome.Interrupted)
            {
                interrupted = true;
                break;
            }

            foreach (var toolTurn in outcome.ToolResultTurns)
            {
                pending.Enqueue(toolTurn);
            }
        }

        if (interrupted || cancellationToken.IsCancellationRequested)
        {
            return new EphemeralRunResult(ReplySent: _sessionContext.ReplySent, UsedFallback: false, Iterations: iterations);
        }

        if (_sessionContext.ReplySent)
        {
            return new EphemeralRunResult(ReplySent: true, UsedFallback: false, Iterations: iterations);
        }

        var fallbackTurn = FindFallbackTurn();
        if (fallbackTurn is null)
        {
            LogNoFallbackContent(_reference.AgentId, _sessionContext.SessionId);
            return new EphemeralRunResult(ReplySent: false, UsedFallback: false, Iterations: iterations);
        }

        await _eventPublisher.PublishAsync(
            Event.WellKnown.Channel.Message with { Id = _sessionContext.ChannelId },
            new ChannelMessage(fallbackTurn.Content, _sessionContext.Parent.AgentId, _time.GetLocalNow())
            {
                SessionId = _sessionContext.SessionId,
            },
            cancellationToken);

        return new EphemeralRunResult(ReplySent: true, UsedFallback: true, Iterations: iterations);
    }

    private ModelTurn? FindFallbackTurn()
    {
        var turns = _agentContext.Turns;
        for (var i = turns.Count - 1; i >= 0; i--)
        {
            var turn = turns[i];
            if (turn.Role == ModelRole.Assistant
                && !string.IsNullOrEmpty(turn.Content)
                && turn.ToolCalls.IsDefaultOrEmpty)
            {
                return turn;
            }
        }
        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }
        await _scope.DisposeAsync();
    }

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Ephemeral session for agent '{AgentId}' session '{SessionId}' hit iteration cap at {Iterations}; will use fallback path if needed.")]
    private partial void LogIterationCapHit(string agentId, Guid sessionId, int iterations);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Ephemeral session for agent '{AgentId}' session '{SessionId}' produced no assistant content turn and never invoked session_reply; parent will not receive a reply.")]
    private partial void LogNoFallbackContent(string agentId, Guid sessionId);
}
