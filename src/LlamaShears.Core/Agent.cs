using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Memory;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent, IEventHandler<ChannelMessage>, IAsyncDisposable
{
    // Channel id portion of the agent's default SessionId. Wiring more
    // channels (and routing inbound events to per-channel sessions)
    // lands in a follow-up; for now every agent has exactly one
    // session and it lives at this slot.
    private const string DefaultChannel = "default";

    private readonly AgentConfig _config;
    private readonly ILanguageModel _model;
    private readonly ILogger _logger;
    private readonly IAgentContext _agentContext;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly ISessionQueue _sessionQueue;
    private readonly CancellationTokenSource _shutdown;
    private readonly Task _loop;
    private readonly SemaphoreSlim _processGate = new(1, 1);
    private readonly Lock _interruptLock = new();
    private CancellationTokenSource? _activeTurnCts;
    private readonly IEventPublisher _eventPublisher;
    private readonly IContextCompactor _compactor;
    private readonly ModelConfiguration _modelConfiguration;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IInferenceRunner _inferenceRunner;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly IPromptContextProvider _promptContext;
    private readonly IMemorySearcher _memorySearcher;
    private readonly ImmutableArray<ToolGroup> _tools;
    private readonly IServiceProvider _scopedServices;
    // Boxed reference to the AsyncServiceScope passed at construction.
    // Held as IAsyncDisposable + interlocked-null on dispose so the
    // scope teardown can't recurse if a service inside the scope ever
    // tries to dispose the agent during its own disposal.
    private IAsyncDisposable? _scope;


    public Agent(
        AgentConfig config,
        ILanguageModel model,
        IAgentContext agentContext,
        ILoggerFactory loggerFactory,
        IEventBus bus,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IContextCompactor compactor,
        ModelConfiguration modelConfiguration,
        IAgentContextProvider agentContextProvider,
        IEventPublisher eventPublisher,
        IInferenceRunner inferenceRunner,
        ICurrentAgentAccessor currentAgent,
        IPromptContextProvider promptContext,
        IMemorySearcher memorySearcher,
        ISessionFactory sessionFactory,
        AsyncServiceScope scope,
        ImmutableArray<ToolGroup> tools = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Id);

        _config = config;
        _model = model;
        _logger = loggerFactory.CreateLogger($"{typeof(Agent).FullName}:{config.Id}");
        _eventPublisher = eventPublisher;
        _agentContext = agentContext;
        _systemPrompt = systemPromptProvider;
        _time = timeProvider;
        _compactor = compactor;
        _modelConfiguration = modelConfiguration;
        _agentContextProvider = agentContextProvider;
        _inferenceRunner = inferenceRunner;
        _currentAgent = currentAgent;
        _promptContext = promptContext;
        _memorySearcher = memorySearcher;
        _tools = tools.IsDefault ? [] : tools;
        _scope = scope;
        _scopedServices = scope.ServiceProvider;
        _sessionQueue = sessionFactory.Get(new SessionId(config.Id, DefaultChannel));
        _shutdown = new CancellationTokenSource();
        _subscription = bus.Subscribe(
            $"{Event.WellKnown.Channel.Message}:+",
            EventDeliveryMode.Awaited,
            this);
        _loop = Task.Run(() => RunLoopAsync(_shutdown.Token));
    }

    public string Id => _config.Id;

    public DateTimeOffset? LastActivity
        => _agentContext.Turns is [.., var last] ? last.Timestamp : null;

    public Task LockAsync(CancellationToken cancellationToken)
        => _processGate.WaitAsync(cancellationToken);

    public ValueTask UnlockAsync()
    {
        _processGate.Release();
        return ValueTask.CompletedTask;
    }

    public Task InterruptAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Snapshot the CTS under the lock and Cancel outside it.
        // CancellationTokenSource.Cancel can run callbacks synchronously,
        // and any callback that touched _interruptLock would deadlock.
        // The interrupt only signals the linked per-turn CTS; the
        // run-loop's outer CT is unaffected, so the agent stays up
        // and resumes on the next ChannelMessage.
        CancellationTokenSource? cancellationTokenSource;
        lock (_interruptLock)
        {
            cancellationTokenSource = _activeTurnCts;
        }
        cancellationTokenSource?.Cancel();
        return Task.CompletedTask;
    }

    public async Task RequestCompactionAsync(CancellationToken cancellationToken)
    {
        await _processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var turns = _agentContext.Turns;
            var systemBody = await _systemPrompt.GetAsync(_config.SystemPrompt, BuildSystemPromptParameters(), cancellationToken).ConfigureAwait(false);
            var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());
            var prompt = new ModelPrompt([systemTurn, .. turns]);
            var snapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
            await _compactor.CompactAsync(snapshot, prompt, _model, _modelConfiguration, force: true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _processGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        var scope = Interlocked.Exchange(ref _scope, null);
        if (scope is null)
        {
            return;
        }

        _subscription.Dispose();
        _shutdown.Cancel();
        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        _shutdown.Dispose();
        _processGate.Dispose();

        await scope.DisposeAsync().ConfigureAwait(false);
    }

    public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask HandleAsync(IEventEnvelope<ChannelMessage> envelope, CancellationToken cancellationToken)
    {
        var data = envelope.Data;
        if (data is null)
        {
            return;
        }
        if (!string.IsNullOrWhiteSpace(data.AgentId) && !string.Equals(data.AgentId, Id, StringComparison.Ordinal))
        {
            return;
        }
        var turn = new ModelTurn(
            ModelRole.User,
            FormatUserContent(data.Text, data.Timestamp, envelope.Type.Id),
            data.Timestamp,
            ChannelId: envelope.Type.Id)
        {
            Attachments = data.Attachments,
        };
        await _sessionQueue.EnqueueAsync(turn, cancellationToken).ConfigureAwait(false);
    }

    private static string FormatUserContent(string text, DateTimeOffset timestamp, string? channelId)
    {
        var sb = new StringBuilder();
        sb.Append("[timestamp]");
        sb.Append(timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        sb.Append("[/timestamp]\n");
        if (!string.IsNullOrEmpty(channelId))
        {
            sb.Append("[sourceChannel]");
            sb.Append(channelId);
            sb.Append("[/sourceChannel]\n");
        }
        sb.Append(text);
        return sb.ToString();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var loggingScope = _logger.BeginScope("{AgentId}", Id);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var batch = await _sessionQueue.DequeueBatchAsync(cancellationToken).ConfigureAwait(false);
                if (batch.IsDefaultOrEmpty)
                {
                    return;
                }

                var correlationId = Guid.CreateVersion7();
                using var innerLoggingScope = _logger.BeginScope("{AgentTurnId}", correlationId);
                await _processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                using var turnCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                lock (_interruptLock)
                {
                    _activeTurnCts = turnCts;
                }
                try
                {
                    await ProcessIterationAsync(batch, correlationId, cancellationToken, turnCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (turnCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                {
                    LogTurnInterrupted(_logger, Id, correlationId);
                }
                finally
                {
                    lock (_interruptLock)
                    {
                        _activeTurnCts = null;
                    }
                    _processGate.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogAgentStopping(Id);
                return;
            }
            catch (Exception ex)
            {
                LogProcessOnceFailed(Id, ex);
            }
        }
    }

    // One inference round-trip. The session queue is the single source
    // of truth for everything the model still needs to see — user
    // batches and tool-result turns both arrive via DequeueBatchAsync,
    // already in strict-provider order (tools first, then a same-channel
    // user batch). Each iteration:
    //   1. publishes the dequeued batch into context (events → persister);
    //   2. refreshes memories iff a new user query landed;
    //   3. runs inference;
    //   4. on outcome with tool calls, enqueues tool result turns so the
    //      next outer dequeue picks them up immediately;
    //   5. on outcome without tool calls, returns and the outer loop
    //      blocks for the next user batch.
    private async Task ProcessIterationAsync(
        ImmutableArray<ModelTurn> batch,
        Guid correlationId,
        CancellationToken outerCancellationToken,
        CancellationToken cancellationToken)
    {
        await using var loopScope = _scopedServices.CreateAsyncScope();

        // Publish each turn so subscribers (UI, history persister) see
        // them and the persister appends them into _agentContext.
        // Outer CT so an /interrupt issued mid-publish can't drop user
        // input that's already been dequeued.
        foreach (var turn in batch)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = Id },
                turn,
                correlationId,
                outerCancellationToken).ConfigureAwait(false);
        }

        // Memory: re-search only when a new user query arrived in this
        // batch. Tool-only batches reuse the cached memories from the
        // user turn that started the conversation.
        var latestUser = LatestUserTurn(batch);
        var memories = latestUser is null ? [] : await SearchMemoriesAsync(latestUser.Content, cancellationToken).ConfigureAwait(false);

        var systemBody = await _systemPrompt.GetAsync(_config.SystemPrompt, BuildSystemPromptParameters(), cancellationToken).ConfigureAwait(false);
        var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());

        // Loopback bearer minting reads from ICurrentAgentAccessor; the
        // scope must be active before tool dispatch fires. Inference
        // and any tool dispatch it spawns complete before this method
        // returns, so a per-iteration scope covers everything.
        var agentInfo = new AgentInfo(
            AgentId: Id,
            ModelId: _modelConfiguration.ModelId,
            ContextWindowSize: _modelConfiguration.ContextLength ?? 0);
        using var agentScope = _currentAgent.BeginScope(agentInfo);

        var turns = _agentContext.Turns;
        var prompt = new ModelPrompt([systemTurn, .. turns]);
        var agentContextSnapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
        prompt = await _compactor.CompactAsync(agentContextSnapshot, prompt, _model, _modelConfiguration, force: false, cancellationToken).ConfigureAwait(false);
        prompt = await InjectPromptContextAsync(prompt, ResolveAnchorChannelId(prompt.Turns), memories, cancellationToken).ConfigureAwait(false);

        var outcome = await _inferenceRunner.RunAsync(
            eventId: Id,
            model: _model,
            prompt: prompt,
            options: new PromptOptions(Tools: _tools),
            emitTurns: true,
            correlationId: correlationId,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // On interrupt, the inference runner has already flushed
        // partial fragments and persisted the assistant turn (with
        // whatever ToolCalls had been dispatched at cancel time).
        // Pair every dispatched tool_call with the synthetic error
        // result the runner collapsed it into, log, then exit. The
        // run-loop returns to its idle wait; the next inbound message
        // resumes the agent. Post-interrupt persistence runs on a fresh
        // CTS with a fixed budget so a wedged subscriber can't make
        // /interrupt hang the agent.
        using var interruptedTokenSource = outcome.Interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var publishToken = outcome.Interrupted ? interruptedTokenSource!.Token : cancellationToken;

        if (outcome.TokenCount is { } tokens)
        {
            await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens), publishToken).ConfigureAwait(false);
        }

        if (outcome.Interrupted)
        {
            // Publish tool turns directly — bypassing the queue —
            // because we don't want another inference iteration. The
            // assistant tool_calls in context still get paired tool
            // results so the persisted history stays strict-provider
            // legal for whatever future user message resumes the
            // session.
            if (!outcome.ToolCalls.IsDefaultOrEmpty)
            {
                await PublishToolTurnsAsync(outcome.ToolCalls, outcome.ToolResults, correlationId, publishToken).ConfigureAwait(false);
            }
            LogTurnInterrupted(_logger, Id, correlationId);
            return;
        }

        if (outcome.ToolCalls.IsDefaultOrEmpty)
        {
            if (outcome.Content.Length == 0)
            {
                LogEmptyResponse(_logger, Id);
            }
            return;
        }
        
        for (var i = 0; i < outcome.ToolCalls.Length; i++)
        {
            var toolTurn = new ModelTurn(
                ModelRole.Tool,
                outcome.ToolResults[i].Content,
                _time.GetLocalNow())
            {
                ToolCall = outcome.ToolCalls[i],
                IsError = outcome.ToolResults[i].IsError,
            };
            await _sessionQueue.EnqueueAsync(toolTurn, cancellationToken).ConfigureAwait(false);
        }
    }

    private static ModelTurn? LatestUserTurn(ImmutableArray<ModelTurn> batch) => batch.Where(i => i.Role == ModelRole.User).Reverse().FirstOrDefault();

    private async Task PublishToolTurnsAsync(
        ImmutableArray<ToolCall> calls,
        ImmutableArray<ToolCallResult> results,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < calls.Length; i++)
        {
            var toolTurn = new ModelTurn(
                ModelRole.Tool,
                results[i].Content,
                _time.GetLocalNow())
            {
                ToolCall = calls[i],
                IsError = results[i].IsError,
            };
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = Id },
                toolTurn,
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }
    }

    // Budget for flushing post-interrupt persistence (token count
    // append, paired tool turns) after a per-turn cancel. See
    // InferenceRunner for the same pattern on partial-fragment
    // finalize publishes and synthetic tool-result publishes.
    private static readonly TimeSpan _interruptFinalizeTimeout = TimeSpan.FromSeconds(5);

    // The ephemeral system block anchors against the latest contiguous
    // run of user turns. Walk back from the tail to the most recent
    // User, then keep walking through any preceding Users to find the
    // first turn of that batch. The ephemeral lands immediately before
    // it. ChannelId for the block is taken from that anchoring user.
    private static int FindLatestUserBatchStart(IReadOnlyList<ModelTurn> turns)
    {
        var i = turns.Count - 1;
        while (i >= 0 && turns[i].Role != ModelRole.User)
        {
            i--;
        }
        if (i < 0)
        {
            return -1;
        }
        while (i - 1 >= 0 && turns[i - 1].Role == ModelRole.User)
        {
            i--;
        }
        return i;
    }

    private static string? ResolveAnchorChannelId(IReadOnlyList<ModelTurn> turns)
    {
        var anchor = FindLatestUserBatchStart(turns);
        return anchor < 0 ? null : turns[anchor].ChannelId;
    }

    private async Task<ModelPrompt> InjectPromptContextAsync(
        ModelPrompt prompt,
        string? channelId,
        IReadOnlyList<PromptContextMemory> memories,
        CancellationToken cancellationToken)
    {
        var now = _time.GetLocalNow();
        var parameters = new PromptContextParameters(
            Now: now.ToString("o", CultureInfo.InvariantCulture),
            Timezone: TimeZoneInfo.Local.Id,
            DayOfWeek: now.DayOfWeek.ToString(),
            ChannelId: channelId,
            ImportantMessage: null,
            WorkspacePath: _config.WorkspacePath)
        {
            Memories = memories,
        };
        var body = await _promptContext.GetAsync(_config.PromptContext, parameters, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            return prompt;
        }

        var turns = prompt.Turns;
        var anchor = FindLatestUserBatchStart(turns);
        if (anchor < 0)
        {
            // No user turn to anchor to (shouldn't happen during a
            // user-driven cycle); leave the prompt untouched rather
            // than dangle an ephemeral block in the air.
            return prompt;
        }

        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, body, now, Ephemeral: true);
        var augmented = new List<ModelTurn>(turns.Count + 1);
        for (var i = 0; i < turns.Count; i++)
        {
            if (i == anchor)
            {
                augmented.Add(ephemeral);
            }
            augmented.Add(turns[i]);
        }
        return new ModelPrompt(augmented);
    }

    private async ValueTask<IReadOnlyList<PromptContextMemory>> SearchMemoriesAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var hits = await _memorySearcher
            .SearchAsync(_config.Id, query, limit: null, minScore: null, cancellationToken)
            .ConfigureAwait(false);
        return [.. hits.Select(i => new PromptContextMemory(i.RelativePath, i.Summary, i.Score))];
    }

    private SystemPromptTemplateParameters BuildSystemPromptParameters() =>
        new(
            AgentId: _config.Id,
            WorkspacePath: _config.WorkspacePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' received an empty response from the model.")]
    private static partial void LogEmptyResponse(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' is stopping.")]
    private partial void LogAgentStopping(string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Agent '{AgentId}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(string agentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' turn '{CorrelationId}' interrupted; partial fragments dropped, agent remains live.")]
    private static partial void LogTurnInterrupted(ILogger logger, string agentId, Guid correlationId);
}
