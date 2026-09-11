using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core;

public sealed class SubagentRunner : ISubagentRunner
{
    private readonly IDataContextScope _dataScope;
    private readonly IPromptedAgentSpawner _spawner;
    private readonly IEventBus _eventBus;
    private readonly TimeProvider _time;

    public SubagentRunner(
        IDataContextScope dataScope,
        IPromptedAgentSpawner spawner,
        IEventBus eventBus,
        TimeProvider time)
    {
        _dataScope = dataScope;
        _spawner = spawner;
        _eventBus = eventBus;
        _time = time;
    }

    /// <inheritdoc />
    public async ValueTask<SubagentRunResult> RunAsync(
        SubagentRunRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_dataScope.TryGetAgentConfig() is not { } parent)
        {
            return Refuse("Refused: subagent_run requires an authenticated agent on the request.");
        }

        if (!_dataScope.IsRootSession())
        {
            return Refuse("Refused: nested sub-agents are not allowed.");
        }

        if (string.IsNullOrWhiteSpace(request.Prompt))
        {
            return Refuse("Refused: prompt is required.");
        }

        if (request.TimeoutSeconds is < SubagentRunRequest.MinTimeoutSeconds
            or > SubagentRunRequest.MaxTimeoutSeconds)
        {
            return Refuse(
                $"Refused: timeoutSeconds must be between {SubagentRunRequest.MinTimeoutSeconds} and {SubagentRunRequest.MaxTimeoutSeconds}.");
        }

        if (request.MaxTurns is { } maxTurns
            && maxTurns is < SubagentRunRequest.MinMaxTurns or > SubagentRunRequest.MaxMaxTurns)
        {
            return Refuse(
                $"Refused: maxTurns must be between {SubagentRunRequest.MinMaxTurns} and {SubagentRunRequest.MaxMaxTurns}.");
        }

        CompositeIdentity? modelId = null;
        if (request.Model is not null)
        {
            if (!CompositeIdentity.TryParse(request.Model, out modelId))
            {
                return Refuse(
                    $"Refused: '{request.Model}' is not a valid model identity; expected '<provider>/<model>'.");
            }
        }

        var config = PromptedAgentStartInformation.CreateDefaultSubAgentConfig("subagent", parent);
        if (modelId is not null)
        {
            config = config with { Model = config.Model with { Id = modelId } };
        }

        if (request.MaxTurns is { } turns)
        {
            config = config with { Tools = config.Tools with { TurnLimit = turns } };
        }

        var parentPath = _dataScope.GetSessionPath();
        var childSession = SessionId.CreateFor(parent.Id, $"subagent-{Guid.CreateVersion7():n}");
        var promptText = string.IsNullOrWhiteSpace(request.Context)
            ? request.Prompt
            : $"{request.Context.Trim()}\n\n{request.Prompt}";
        var initialPrompt = new ModelTurn(
            ModelRole.User,
            promptText,
            _time.GetUtcNow(),
            $"subagent:{childSession.Name}");
        var contextData = ImmutableDictionary.CreateRange(
        [
            new KeyValuePair<string, object?>(
                TransientAgentReportPolicy.DataKey,
                new TransientAgentReportPolicy(ReportToParent: !request.AwaitResult)),
        ]);
        var startInfo = new PromptedAgentStartInformation(
            Config: config,
            Id: childSession,
            ParentSessionPath: parentPath,
            InitialPrompt: initialPrompt,
            ContextData: contextData);

        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        string? lastAssistant = null;
        using var idleSubscription = _eventBus.Subscribe<AgentLifecycleEvent>(
            Event.WellKnown.Agent.Idle with { Id = childSession },
            EventDeliveryMode.Awaited,
            (_, _) =>
            {
                idle.TrySetResult();
                return ValueTask.CompletedTask;
            });
        using var turnSubscription = _eventBus.Subscribe<ModelTurn>(
            Event.WellKnown.Agent.Turn with { Id = childSession },
            EventDeliveryMode.Awaited,
            (envelope, _) =>
            {
                if (envelope.Data is { Role: ModelRole.Assistant } turn)
                {
                    lastAssistant = turn.Content;
                }

                return ValueTask.CompletedTask;
            });

        try
        {
            await _spawner.CreateAsync(startInfo, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new SubagentRunResult(
                Ok: false,
                SessionId: childSession.ToString(),
                Awaited: request.AwaitResult,
                Output: null,
                TimedOut: false,
                Error: $"Failed to start sub-agent: {ex.Message}",
                Started: false);
        }

        if (!request.AwaitResult)
        {
            return new SubagentRunResult(
                Ok: false,
                SessionId: childSession.ToString(),
                Awaited: false,
                Output: null,
                TimedOut: false,
                Error: null,
                Started: true);
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(request.TimeoutSeconds));
        try
        {
            await idle.Task.WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await PublishStopAsync(childSession, cancellationToken);
            throw;
        }
        catch (OperationCanceledException)
        {
            await PublishStopAsync(childSession, cancellationToken);
            return new SubagentRunResult(
                Ok: false,
                SessionId: childSession.ToString(),
                Awaited: true,
                Output: lastAssistant,
                TimedOut: true,
                Error: $"Timed out after {request.TimeoutSeconds}s waiting for the sub-agent.",
                Started: true);
        }

        return new SubagentRunResult(
            Ok: true,
            SessionId: childSession.ToString(),
            Awaited: true,
            Output: lastAssistant,
            TimedOut: false,
            Error: null,
            Started: true);
    }

    private ValueTask PublishStopAsync(SessionId childSession, CancellationToken cancellationToken)
        => _eventBus.PublishAsync(
            Event.WellKnown.Command.AgentStop with { Id = childSession },
            new AgentStopRequest(childSession),
            cancellationToken);

    private static SubagentRunResult Refuse(string error) =>
        new(
            Ok: false,
            SessionId: null,
            Awaited: false,
            Output: null,
            TimedOut: false,
            Error: error);
}
