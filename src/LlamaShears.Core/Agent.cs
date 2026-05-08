using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading.Channels;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
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
    private readonly AgentConfig _config;
    private readonly ILanguageModel _model;
    private readonly ILogger _logger;
    private readonly IAgentContext _agentContext;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly Channel<ModelTurn> _inbound;
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
    private readonly IToolCallDispatcher _toolDispatcher;
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

    // When Memory.Prefetch is enabled, a search task is installed by
    // HandleAsync the moment a ChannelMessage is observed and consumed
    // by the run loop on entry to the next inference cycle. Only one
    // is in flight at a time — if more messages arrive during the same
    // window they coalesce onto the existing task rather than racing
    // fresh embeddings.
    private Task<IReadOnlyList<PromptContextMemory>>? _prefetchTask;


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
        IToolCallDispatcher toolDispatcher,
        ICurrentAgentAccessor currentAgent,
        IPromptContextProvider promptContext,
        IMemorySearcher memorySearcher,
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
        _toolDispatcher = toolDispatcher;
        _currentAgent = currentAgent;
        _promptContext = promptContext;
        _memorySearcher = memorySearcher;
        _tools = tools.IsDefault ? [] : tools;
        _scope = scope;
        _scopedServices = scope.ServiceProvider;
        _inbound = Channel.CreateUnbounded<ModelTurn>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });
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
        // Interlocked-null guards against re-entrant disposal: a service
        // resolved from this scope that holds a back-reference to the
        // agent could otherwise drag dispose into a loop on its own
        // teardown. First disposer wins; subsequent calls observe null
        // and return immediately.
        var scope = Interlocked.Exchange(ref _scope, null);
        if (scope is null)
        {
            return;
        }

        _subscription.Dispose();
        _shutdown.Cancel();
        _inbound.Writer.TryComplete();
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
        if (_config.Memory.Prefetch && !string.IsNullOrWhiteSpace(data.Text))
        {
            TryStartPrefetch(data.Text);
        }
        var turn = new ModelTurn(
            ModelRole.User,
            data.Text,
            data.Timestamp,
            ChannelId: envelope.Type.Id)
        {
            Attachments = data.Attachments,
        };
        await _inbound.Writer.WriteAsync(turn, cancellationToken).ConfigureAwait(false);
    }

    private void TryStartPrefetch(string text)
    {
        // CompareExchange so the first message in a window wins; later
        // messages don't fire redundant embedding round-trips. The
        // prefetch's lifetime is tied to agent shutdown, not to whatever
        // CT delivered the channel event.
        var task = RunPrefetchAsync(text, _shutdown.Token);
        if (Interlocked.CompareExchange(ref _prefetchTask, task, null) is not null)
        {
            // Another prefetch is already in flight. Observe to avoid
            // an UnobservedTaskException if it faults.
            _ = task.ContinueWith(static t => _ = t.Exception, TaskScheduler.Default);
        }
    }

    private async Task<IReadOnlyList<PromptContextMemory>> RunPrefetchAsync(string text, CancellationToken cancellationToken)
    {
        var stub = new ModelTurn(ModelRole.User, text, _time.GetLocalNow());
        return await SearchMemoriesAsync(stub, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var loggingScope = _logger.BeginScope("{AgentId}", Id);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var batch = await WaitForNextBatchAsync(cancellationToken).ConfigureAwait(false);
                if (batch.Count == 0)
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
                    await ProcessTurnAsync(batch, correlationId, cancellationToken, turnCts.Token).ConfigureAwait(false);
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

    // Blocks for the first message, then drains every subsequent message
    // in the channel that shares the same ChannelId. The same-channel rule
    // mirrors the prior batching behavior (which keyed off envelope.Type)
    // and keeps batches scoped to a single conversational source.
    private async Task<List<ModelTurn>> WaitForNextBatchAsync(CancellationToken cancellationToken)
    {
        var reader = _inbound.Reader;
        if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return [];
        }
        if (!reader.TryRead(out var first))
        {
            return [];
        }
        return DrainSameChannel(first);
    }

    // Non-blocking drain. Returns an empty list when nothing is queued.
    // Used between inference iterations to fold in user messages that
    // arrived during tool dispatch — they land contiguously after the
    // tool result turns and before the next assistant turn, which is
    // the only ordering strict providers (OpenAI, Anthropic) accept.
    private List<ModelTurn> TryGetNextBatch()
    {
        var reader = _inbound.Reader;
        if (!reader.TryRead(out var first))
        {
            return [];
        }
        return DrainSameChannel(first);
    }

    private List<ModelTurn> DrainSameChannel(ModelTurn first)
    {
        var batch = new List<ModelTurn> { first };
        var reader = _inbound.Reader;
        while (reader.TryPeek(out var next)
            && string.Equals(next.ChannelId, first.ChannelId, StringComparison.Ordinal))
        {
            if (!reader.TryRead(out var taken))
            {
                break;
            }
            batch.Add(taken);
        }
        return batch;
    }

    private async Task ProcessTurnAsync(
        IReadOnlyList<ModelTurn> initialBatch,
        Guid correlationId,
        CancellationToken outerCancellationToken,
        CancellationToken cancellationToken)
    {
        // Nested per-turn scope: every tool call and inner inference
        // iteration in this run sees the same scoped instances; the next
        // top-level turn starts fresh. Resolve scoped services from
        // loopScope.ServiceProvider when they need to land here.
        await using var loopScope = _scopedServices.CreateAsyncScope();

        // Initial batch: publish each user turn so subscribers (UI,
        // history persister) see them and the persister appends them
        // into _agentContext. Use the outer cancellation token so an
        // /interrupt issued mid-publish can't drop the user input that
        // is already off the queue.
        await PublishUserBatchAsync(initialBatch, correlationId, outerCancellationToken).ConfigureAwait(false);

        var systemBody = await _systemPrompt.GetAsync(_config.SystemPrompt, BuildSystemPromptParameters(), cancellationToken).ConfigureAwait(false);
        var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());

        // Memory query keyed off the latest user message we've seen.
        // Initial entry consumes the prefetch (if armed); mid-loop user
        // arrivals re-search inline against their newest message.
        var memories = await ConsumePrefetchOrSearchAsync(initialBatch[^1], cancellationToken).ConfigureAwait(false);

        // Loopback bearer minting reads from ICurrentAgentAccessor; the
        // scope must be active before tool dispatch fires, and dispatch
        // starts inside the inference runner the moment a tool call
        // arrives (so it overlaps with continued model streaming).
        // Hoisting the scope to the whole turn keeps every dispatched
        // tool — across every iteration of the inference loop — seeing
        // the right agent. AsyncLocal flows into the inner tasks because
        // they capture the current ExecutionContext at start.
        var agentInfo = new AgentInfo(
            AgentId: Id,
            ModelId: _modelConfiguration.ModelId,
            ContextWindowSize: _modelConfiguration.ContextLength ?? 0);
        using var agentScope = _currentAgent.BeginScope(agentInfo);

        while (true)
        {
            var turns = _agentContext.Turns;
            var prompt = new ModelPrompt([systemTurn, .. turns]);
            var agentContextSnapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
            prompt = await _compactor.CompactAsync(agentContextSnapshot, prompt, _model, _modelConfiguration, force: false, cancellationToken).ConfigureAwait(false);
            prompt = FormatUserTurns(prompt);
            prompt = await InjectPromptContextAsync(prompt, ResolveAnchorChannelId(prompt.Turns), memories, cancellationToken).ConfigureAwait(false);

            ValueTask<ToolCallResult> Dispatcher(ToolCall call, CancellationToken callCancellationToken)
                => DispatchToolAsync(call, correlationId, callCancellationToken);

            var outcome = await _inferenceRunner.RunAsync(
                eventId: Id,
                model: _model,
                prompt: prompt,
                options: new PromptOptions(Tools: _tools),
                emitTurns: true,
                correlationId: correlationId,
                dispatchTool: Dispatcher,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // On interrupt, the inference runner has already flushed
            // partial fragments and persisted the assistant turn (with
            // whatever ToolCalls had been dispatched at cancel time).
            // Pair every dispatched tool_call with the synthetic error
            // result the runner collapsed it into, log, then exit. The
            // run-loop returns to its idle wait; the next inbound
            // message resumes the agent. Post-interrupt persistence
            // runs on a fresh CTS with a fixed budget so a wedged
            // subscriber can't make /interrupt hang the agent.
            using var interruptedTokenSource = outcome.Interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
            var publishToken = outcome.Interrupted ? interruptedTokenSource!.Token : cancellationToken;

            if (outcome.TokenCount is { } tokens)
            {
                await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens), publishToken).ConfigureAwait(false);
            }

            if (!outcome.ToolCalls.IsDefaultOrEmpty)
            {
                // Strict-provider ordering: assistant(tool_calls) is
                // already in context (published by the inference
                // runner). Tool result turns must land contiguously
                // next, in original call order, before any user turn
                // or the next assistant turn.
                await PublishToolTurnsAsync(outcome.ToolCalls, outcome.ToolResults, correlationId, publishToken).ConfigureAwait(false);
            }

            if (outcome.Interrupted)
            {
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

            // Drain any user messages that arrived while the model and
            // tools were running. They append after the tool turns and
            // are visible to the next inference iteration. Use the outer
            // CT so /interrupt can't cause dequeued user input to be
            // dropped on the floor.
            var nextBatch = TryGetNextBatch();
            if (nextBatch.Count > 0)
            {
                await PublishUserBatchAsync(nextBatch, correlationId, outerCancellationToken).ConfigureAwait(false);
                memories = await SearchMemoriesAsync(nextBatch[^1], cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task PublishUserBatchAsync(
        IReadOnlyList<ModelTurn> turns,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < turns.Count; i++)
        {
            await _eventPublisher.PublishAsync(
                Event.WellKnown.Agent.Turn with { Id = Id },
                turns[i],
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task PublishToolTurnsAsync(
        ImmutableArray<ToolCall> calls,
        ImmutableArray<ToolCallResult> results,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        // Persist Tool turns in original call order. Re-prompt history
        // stays deterministic regardless of which tool finished first,
        // which matters for any model that pairs tool_calls to
        // tool_results positionally rather than by id.
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

    private async ValueTask<ToolCallResult> DispatchToolAsync(
        ToolCall call,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        ToolCallResult result;
        var interrupted = false;
        try
        {
            result = await _toolDispatcher.DispatchAsync(call, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Synthesize an error result so the caller can pair every
            // tool_call with a tool_result in persisted history. Strict
            // providers (OpenAI, Anthropic) reject any assistant
            // tool_call that has no matching tool_result on the next
            // re-prompt; an interrupted call counts as failed, not
            // skipped.
            result = new ToolCallResult("Tool call interrupted by user.", IsError: true);
            interrupted = true;
        }
        // Publish the result event the moment dispatch completes. This
        // can land *while* the model is still streaming subsequent tool
        // calls — that's intentional. The UI sees results in real
        // arrival order; the conversation history is still assembled in
        // call order in PublishToolTurnsAsync. On interrupt, fall back
        // to a fresh CTS with a fixed budget — long enough for the
        // in-process persister/UI to record the failure, short enough
        // that a wedged subscriber can't make /interrupt look like a
        // hang.
        using var interruptedTokenSource = interrupted ? new CancellationTokenSource(_interruptFinalizeTimeout) : null;
        var publishToken = interrupted ? interruptedTokenSource!.Token : cancellationToken;
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.ToolResult with { Id = Id },
            new AgentToolResultFragment(
                call.Source,
                call.Name,
                result.Content,
                result.IsError,
                call.CallId),
            correlationId,
            publishToken).ConfigureAwait(false);
        return result;
    }

    // Budget for flushing the synthetic interrupt-failure tool-result
    // event after a per-turn cancel. See InferenceRunner for the same
    // pattern on partial-fragment finalize publishes.
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

    // Per-channel-message format applied at prompt-build, not at storage.
    // Storage keeps user content clean so a future format change doesn't
    // rewrite history; replays render with the formatter active at the
    // time of the replay. System / SystemEphemeral / Assistant / Thought
    // / Tool / FrameworkUser / FrameworkAssistant are left untouched.
    private static ModelPrompt FormatUserTurns(ModelPrompt prompt)
    {
        var turns = prompt.Turns;
        List<ModelTurn>? rebuilt = null;
        for (var i = 0; i < turns.Count; i++)
        {
            var turn = turns[i];
            if (turn.Role != ModelRole.User)
            {
                rebuilt?.Add(turn);
                continue;
            }
            rebuilt ??= new List<ModelTurn>(turns.Count);
            for (var j = rebuilt.Count; j < i; j++)
            {
                rebuilt.Add(turns[j]);
            }
            rebuilt.Add(turn with { Content = FormatUserContent(turn) });
        }
        return rebuilt is null ? prompt : new ModelPrompt(rebuilt);
    }

    private static string FormatUserContent(ModelTurn turn)
    {
        var sb = new StringBuilder();
        sb.Append("[timestamp]");
        sb.Append(turn.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        sb.Append("[/timestamp]\n");
        if (!string.IsNullOrEmpty(turn.ChannelId))
        {
            sb.Append("[sourceChannel]");
            sb.Append(turn.ChannelId);
            sb.Append("[/sourceChannel]\n");
        }
        sb.Append(turn.Content);
        return sb.ToString();
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

        var ephemeral = new ModelTurn(ModelRole.SystemEphemeral, body, now);
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

    private const int MemorySearchLimit = 5;
    // Empirical with embeddinggemma:latest + task prefixes: relevant
    // matches land in ~0.40-0.60, unrelated noise stays under ~0.10.
    // Threshold sits comfortably in the gap; short queries occasionally
    // skim 0.40, so anything above 0.30 is conservative-but-safe.
    private const double MemorySearchMinScore = 0.30;

    private async ValueTask<IReadOnlyList<PromptContextMemory>> ConsumePrefetchOrSearchAsync(
        ModelTurn userTurn,
        CancellationToken cancellationToken)
    {
        var pending = Interlocked.Exchange(ref _prefetchTask, null);
        if (pending is not null)
        {
            // Best-effort: if the prefetch faulted, fall through to the
            // inline search rather than deny memories for this turn.
            try
            {
                return await pending.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                LogMemorySearchFailed(_logger, Id, ex.Message, ex);
            }
        }
        return await SearchMemoriesAsync(userTurn, cancellationToken).ConfigureAwait(false);
    }

    // Build a query out of the last assistant turn (if any) plus the
    // freshly-arrived user turn, then surface the matching memory
    // bodies into the per-turn ephemeral block. Best-effort: if the
    // embedding model is unreachable, the misconfiguration is logged
    // and the turn proceeds without memory enrichment.
    private async ValueTask<IReadOnlyList<PromptContextMemory>> SearchMemoriesAsync(
        ModelTurn userTurn,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_config.WorkspacePath))
        {
            return [];
        }

        var query = BuildMemoryQuery(userTurn);
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        try
        {
            var hits = await _memorySearcher
                .SearchAsync(_config.Id, query, MemorySearchLimit, MemorySearchMinScore, cancellationToken)
                .ConfigureAwait(false);
            if (hits.Count == 0)
            {
                return [];
            }

            var results = new List<PromptContextMemory>(hits.Count);
            foreach (var hit in hits)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = Path.Combine(_config.WorkspacePath, hit.RelativePath);
                if (!File.Exists(fullPath))
                {
                    continue;
                }
                var summary = await ReadFirstLineAsync(fullPath, cancellationToken).ConfigureAwait(false);
                results.Add(new PromptContextMemory(hit.RelativePath, summary, hit.Score));
            }
            return results;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogMemorySearchFailed(_logger, Id, ex.Message, ex);
            return [];
        }
    }

    // Surfaces only the first line of each memory file into the per-turn
    // ephemeral block. Authors are expected to write a meaningful first
    // line (typically a markdown H1 title); the model can pull the full
    // body on demand via file_read.
    private static async ValueTask<string> ReadFirstLineAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        return line ?? string.Empty;
    }

    private string BuildMemoryQuery(ModelTurn userTurn)
    {
        ModelTurn? lastAssistant = null;
        var turns = _agentContext.Turns;
        for (var i = turns.Count - 1; i >= 0; i--)
        {
            if (turns[i].Role == ModelRole.Assistant)
            {
                lastAssistant = turns[i];
                break;
            }
        }
        if (lastAssistant is null)
        {
            return userTurn.Content;
        }
        return $"{lastAssistant.Content}\n\n{userTurn.Content}";
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Memory search failed for agent '{AgentId}': {Message}. Proceeding without memory enrichment.")]
    private static partial void LogMemorySearchFailed(ILogger logger, string agentId, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' turn '{CorrelationId}' interrupted; partial fragments dropped, agent remains live.")]
    private static partial void LogTurnInterrupted(ILogger logger, string agentId, Guid correlationId);
}
