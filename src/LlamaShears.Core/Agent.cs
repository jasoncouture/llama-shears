using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Threading.Channels;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Content;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.PromptContext;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class Agent : IAgent, IEventHandler<ChannelMessage>, IDisposable
{
    private readonly AgentConfig _config;
    private readonly ILanguageModel _model;
    private readonly ILogger _logger;
    private readonly IAgentContext _agentContext;
    private readonly ISystemPromptProvider _systemPrompt;
    private readonly TimeProvider _time;
    private readonly IDisposable _subscription;
    private readonly Channel<IEventEnvelope<ChannelMessage>> _inbound;
    private readonly CancellationTokenSource _shutdown;
    private readonly Task _loop;
    private readonly SemaphoreSlim _processGate = new(1, 1);
    private readonly IEventPublisher _eventPublisher;
    private readonly IContextCompactor _compactor;
    private readonly ModelConfiguration _modelConfiguration;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IInferenceRunner _inferenceRunner;
    private readonly IToolCallDispatcher _toolDispatcher;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly IPromptContextProvider _promptContext;
    private readonly ImmutableArray<ToolGroup> _tools;


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
        _tools = tools.IsDefault ? [] : tools;
        _inbound = Channel.CreateUnbounded<IEventEnvelope<ChannelMessage>>(new UnboundedChannelOptions
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

    public void Dispose()
    {
        _subscription.Dispose();
        _shutdown.Cancel();
        _inbound.Writer.TryComplete();
        try
        {
            _loop.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
        }
        _shutdown.Dispose();
        _processGate.Dispose();
    }

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
        await _inbound.Writer.WriteAsync(envelope, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var loggingScope = _logger.BeginScope("{AgentId}", Id);
        var reader = _inbound.Reader;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    return;
                }
                if (!reader.TryRead(out var first))
                {
                    continue;
                }
                var batch = new List<IEventEnvelope<ChannelMessage>> { first };
                while (reader.TryPeek(out var next) && next.Type == first.Type)
                {
                    if (!reader.TryRead(out var taken))
                    {
                        break;
                    }
                    batch.Add(taken);
                }

                var correlationId = Guid.CreateVersion7();
                using var innerLoggingScope = _logger.BeginScope("{AgentTurnId}", correlationId);
                await _processGate.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await ProcessBatchAsync(batch, correlationId, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
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

    private async Task ProcessBatchAsync(
        IReadOnlyList<IEventEnvelope<ChannelMessage>> batch,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var userTurn = BuildUserTurn(batch);
        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.Turn with { Id = Id },
            userTurn,
            correlationId,
            cancellationToken).ConfigureAwait(false);

        var systemBody = await _systemPrompt.GetAsync(_config.SystemPrompt, BuildSystemPromptParameters(), cancellationToken).ConfigureAwait(false);
        var systemTurn = new ModelTurn(ModelRole.System, systemBody, _time.GetLocalNow());

        for (var iteration = 0; iteration < _config.Tools.TurnLimit; iteration++)
        {
            // The final iteration runs with no tools so the model has
            // to produce text (or nothing) — TurnLimit=N means N-1
            // tool-calling turns followed by one tools-less wrap-up.
            var isFinalIteration = iteration == _config.Tools.TurnLimit - 1;
            var promptOptions = isFinalIteration
                ? new PromptOptions(Tools: [])
                : new PromptOptions(Tools: _tools);

            var turns = _agentContext.Turns;
            var prompt = new ModelPrompt([systemTurn, .. turns]);
            var agentContextSnapshot = await _agentContextProvider.CreateAgentContextAsync(Id, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Agent context provider returned null for running agent '{Id}'.");
            prompt = await _compactor.CompactAsync(agentContextSnapshot, prompt, _model, _modelConfiguration, force: false, cancellationToken).ConfigureAwait(false);
            var importantMessage = isFinalIteration
                ? "You have exceeded your turn limit. Respond in text — any further tool calls will be ignored. This is your final output before control returns to the user."
                : null;
            prompt = await InjectPromptContextAsync(prompt, batch[^1].Type.Id, importantMessage, cancellationToken).ConfigureAwait(false);

            var outcome = await _inferenceRunner.RunAsync(
                eventId: Id,
                model: _model,
                prompt: prompt,
                options: promptOptions,
                emitTurns: true,
                correlationId: correlationId,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (outcome.TokenCount is { } tokens)
            {
                await _agentContext.AppendAsync(new ModelTokenInformationContextEntry(tokens), cancellationToken).ConfigureAwait(false);
            }

            if (isFinalIteration)
            {
                // Some chat templates confabulate tool calls even when
                // the schema is empty. Drop them on the floor — this
                // turn's contract is "produce text or stop" — and log
                // for diagnostics. Whatever text the model produced is
                // the answer; empty content gets the standard log.
                if (!outcome.ToolCalls.IsDefaultOrEmpty)
                {
                    LogFinalTurnToolCallsDropped(_logger, Id, outcome.ToolCalls.Length);
                }
                if (outcome.Content.Length == 0)
                {
                    LogEmptyResponse(_logger, Id);
                }
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

            await DispatchToolCallsAsync(outcome.ToolCalls, correlationId, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DispatchToolCallsAsync(
        ImmutableArray<ToolCall> calls,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var agentInfo = new AgentInfo(
            AgentId: Id,
            ModelId: _modelConfiguration.ModelId,
            ContextWindowSize: _modelConfiguration.ContextLength ?? 0);

        // Loopback bearer minting reads from ICurrentAgentAccessor;
        // hold the scope across the whole dispatch batch so each
        // outbound MCP call sees the agent it's acting on behalf of.
        // AsyncLocal flows into the parallel tasks below because they
        // capture the current ExecutionContext at start.
        using var scope = _currentAgent.BeginScope(agentInfo);

        // Fan out: each tool runs concurrently and publishes its own
        // ToolResult event the moment it completes, so the UI sees
        // results land in real arrival order.
        var results = new ToolCallResult[calls.Length];
        var dispatchTasks = new Task[calls.Length];
        for (var i = 0; i < calls.Length; i++)
        {
            dispatchTasks[i] = DispatchOneAsync(calls[i], i, results, correlationId, cancellationToken);
        }
        await Task.WhenAll(dispatchTasks).ConfigureAwait(false);

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

    private async Task DispatchOneAsync(
        ToolCall call,
        int index,
        ToolCallResult[] results,
        Guid correlationId,
        CancellationToken cancellationToken)
    {
        var result = await _toolDispatcher.DispatchAsync(call, cancellationToken).ConfigureAwait(false);
        results[index] = result;

        await _eventPublisher.PublishAsync(
            Event.WellKnown.Agent.ToolResult with { Id = Id },
            new AgentToolResultFragment(
                call.Source,
                call.Name,
                result.Content,
                result.IsError,
                call.CallId),
            correlationId,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ModelPrompt> InjectPromptContextAsync(ModelPrompt prompt, string? channelId, string? importantMessage, CancellationToken cancellationToken)
    {
        var now = _time.GetLocalNow();
        var parameters = new PromptContextParameters(
            Now: now.ToString("o", CultureInfo.InvariantCulture),
            Timezone: TimeZoneInfo.Local.Id,
            DayOfWeek: now.DayOfWeek.ToString(),
            ChannelId: channelId,
            ImportantMessage: importantMessage);
        var body = await _promptContext.GetAsync(parameters, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(body))
        {
            // No template (or rendered to empty). The system prompt
            // describes the prefix as optional in that case — we just
            // skip the injection.
            return prompt;
        }

        var turns = prompt.Turns;
        var lastUserIdx = -1;
        for (var i = turns.Count - 1; i >= 0; i--)
        {
            if (turns[i].Role == ModelRole.User)
            {
                lastUserIdx = i;
                break;
            }
        }
        if (lastUserIdx < 0)
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
            if (i == lastUserIdx)
            {
                augmented.Add(ephemeral);
            }
            augmented.Add(turns[i]);
        }
        return new ModelPrompt(augmented);
    }

    private SystemPromptTemplateParameters BuildSystemPromptParameters() =>
        new(
            AgentId: _config.Id,
            WorkspacePath: _config.WorkspacePath,
            ToolCallTurns: _config.Tools.TurnLimit);

    private static ModelTurn BuildUserTurn(IReadOnlyList<IEventEnvelope<ChannelMessage>> batch)
    {
        if (batch.Count == 1)
        {
            var only = batch[0].Data!;
            return new ModelTurn(ModelRole.User, only.Text, only.Timestamp)
            {
                Attachments = only.Attachments,
            };
        }

        var sb = new StringBuilder();
        sb.Append(string.Format(
            CultureInfo.InvariantCulture,
            "The following {0} messages arrived since your last response, in order:",
            batch.Count));
        // Coalesce attachments from every message in the batch onto
        // the merged turn — the model needs all of them alongside the
        // merged text. They're tagged in-text by the bracketed index
        // so the model can correlate "image #2 of [3]" with its line.
        var combined = ImmutableArray.CreateBuilder<Attachment>();
        for (var i = 0; i < batch.Count; i++)
        {
            var msg = batch[i].Data!;
            sb.Append("\n\n[");
            sb.Append((i + 1).ToString(CultureInfo.InvariantCulture));
            sb.Append("] (");
            sb.Append(msg.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
            sb.Append(") ");
            sb.Append(msg.Text);
            if (!msg.Attachments.IsDefaultOrEmpty)
            {
                combined.AddRange(msg.Attachments);
            }
        }
        return new ModelTurn(ModelRole.User, sb.ToString(), batch[^1].Data!.Timestamp)
        {
            Attachments = combined.ToImmutable(),
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' received an empty response from the model.")]
    private static partial void LogEmptyResponse(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Agent '{AgentId}' is stopping.")]
    private partial void LogAgentStopping(string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Agent '{AgentId}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(string agentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Agent '{AgentId}' produced {Count} tool call(s) on the final tools-less turn; dropped.")]
    private static partial void LogFinalTurnToolCallsDropped(ILogger logger, string agentId, int count);
}
