using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentHost : BackgroundService, IEventHandler<AgentStartRequest>,
    IEventHandler<AgentStopRequest>, IAsyncDisposable
{
    private readonly ILogger<AgentHost> _logger;
    private readonly IEventBus _eventPublisher;
    private readonly IAgentInstanceRepository _agentRepository;

    private int _stopped = 0;

    // This is abused, when we take a read lock, we're writing, when we take a write lock, we're shutting down.
    private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
    private readonly IAsyncDisposable _subscriptions;

    public AgentHost(ILogger<AgentHost> logger, IEventBus eventPublisher, IEventBus eventBus,
        IAgentInstanceRepository agentRepository)
    {
        _logger = logger;
        _eventPublisher = eventPublisher;
        _agentRepository = agentRepository;
        _subscriptions =
            eventBus.Subscribe<AgentStartRequest>($"{Event.WellKnown.Command.AgentStart}:+", EventDeliveryMode.Awaited, this)
                .And(eventBus.Subscribe<AgentStopRequest>($"{Event.WellKnown.Command.AgentStop}:+" , EventDeliveryMode.Awaited,
                    this));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var taskCompletionSource = new TaskCompletionSource();
        await using var cancellationTokenRegistration =
            stoppingToken.Register(s => ((TaskCompletionSource)s!).TrySetResult(), taskCompletionSource);

        await taskCompletionSource.Task;
        Interlocked.CompareExchange(ref _stopped, 1, 0);
        var disposalTasks = new List<Task>();
        using var shutdownTimeoutTokenSource = new CancellationTokenSource();
        shutdownTimeoutTokenSource.CancelAfter(TimeSpan.FromSeconds(30));
        var shutdownTimeoutToken = shutdownTimeoutTokenSource.Token;
        await _mutex.WaitAsync(shutdownTimeoutToken);

        LogStoppingAllAgentsBecauseShutdownSignalWasReceived();

        foreach (var agent in _agentRepository.GetAllAgents())
        {
            _agentRepository.RemoveDescendents(agent.SessionPath.Id);
            disposalTasks.Add(ShutdownAgentAsync(agent, shutdownTimeoutToken).AsTask());
        }

        try
        {
            if (disposalTasks.Count != 0)
            {
                LogWaitingForAllAgentsToFinishShutdown();
                await Task.WhenAll(disposalTasks);
                LogAgentShutdownComplete();
            }
        }
        catch (Exception ex)
        {
            LogAnExceptionOccurredDuringAgentHostShutdown(ex);
            // ignored, we're shutting down
        }
    }

    public async ValueTask HandleAsync(IEventEnvelope<AgentStartRequest> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data?.Handle is not { } handle)
            throw new InvalidOperationException(
                "Received an agent start request, but no agent handle was provided");
        if (Interlocked.CompareExchange(ref _stopped, 0, 0) != 0)
        {
            LogReceivedAnAgentStartRequestAfterShutdownDisposingInsteadOfStarting();
            await handle.DisposeAsync();
            throw new ObjectDisposedException(
                $"Host is shutting down, agent {handle.SessionPath.Current} was disposed and was not started");
        }

        if (handle.Started)
        {
            // This will throw if it was already added, if not, this is a failsafe to add what was an orphan agent to the repository
            // An alternative would be to throw here, and that might be the better choice.
            _agentRepository.AddAgent(handle);
            LogAnAgentStartRequestWasReceivedForAnAlreadyStartedSessionButThatSessionWasNotYet(handle.SessionPath
                .Current);
            return;
        }

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (Interlocked.CompareExchange(ref _stopped, 0, 0) != 0)
            {
                LogReceivedAnAgentStartRequestAfterShutdownDisposingInsteadOfStarting();
                await handle.DisposeAsync();
                throw new ObjectDisposedException(
                    $"Host is shutting down, agent {handle.SessionPath.Current} was disposed and was not started");
            }

            LogAttemptingToStartAgentSessionId(handle.SessionPath.Current);
            try
            {
                // Agent repository really needs a self-cleanup too. A garbage collector. We'll get there later I suppose
                _agentRepository.AddAgent(handle);
                handle.Start();
            }
            catch (Exception ex)
            {
                LogFailedToStartAgentId(handle.SessionPath.Current, ex);
                try
                {
                    await handle.DisposeAsync();
                }
                catch (Exception inner)
                {
                    LogErrorStateShutdownFault(handle.SessionPath.Current, inner);
                }

                throw;
            }

            LogAgentIdStarted(handle.SessionPath.Current);
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async ValueTask ShutdownAgentAsync(Guid key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var child in _agentRepository.DescendentsOf(key))
        {
            await ShutdownAgentAsync(child.SessionPath.Id, cancellationToken);
        }

        if (!_agentRepository.Remove(key, out var handle))
        {
            _logger.LogWarning("Tried to remove agent with {Id}, but it was not present in the repository", key);
            throw new InvalidOperationException($"No such agent: {key}");
        }

        _logger.BeginScope("{Session} {ParentSession} {RootSession}",
            handle.SessionPath.Current,
            handle.SessionPath.Parent,
            handle.SessionPath.Root);
        var sessionShutdownTimeoutCancellationTokenSource = new CancellationTokenSource();
        var sessionShutdownTimeoutToken = sessionShutdownTimeoutCancellationTokenSource.Token;
        sessionShutdownTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            LogSendingShutdownEventToId(handle.SessionPath.Current);
            var eventType = Event.WellKnown.Command.AgentShutdown with { Id = handle.SessionPath.Current };
            var eventParameter = new AgentStopRequest(handle.SessionPath.Current);
            await _eventPublisher.PublishAsync(eventType, eventParameter, cancellationToken);
            LogShutdownRequestSentDisposingSessionId(handle.SessionPath.Current);

            await handle.DisposeAsync().AsTask().WaitAsync(sessionShutdownTimeoutToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Shutdown was aborted before it was started for {Session}", handle.SessionPath.Current);
        }
        catch (OperationCanceledException) when (sessionShutdownTimeoutToken.IsCancellationRequested)
        {
            _logger.LogWarning("Session {Session} timed out while disposing, and is in an unknown state",
                handle.SessionPath.Current);
        }
    }

    private async ValueTask ShutdownAgentAsync(SessionId session, CancellationToken cancellationToken)
    {
        await ShutdownAgentAsync(session.Id, cancellationToken);
    }

    private async ValueTask ShutdownAgentAsync(AgentHandle handle, CancellationToken cancellationToken)
    {
        await ShutdownAgentAsync(handle.SessionPath.Current.Id, cancellationToken);
    }

    public async ValueTask HandleAsync(IEventEnvelope<AgentStopRequest> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data?.SessionId is not { } session)
            throw new InvalidOperationException(
                "Received an agent start request, but no agent session was provided");
        LogAttemptingToStopSessionSession(session);
        await _mutex.WaitAsync(cancellationToken);
        Task shutdownTask;
        try
        {
            shutdownTask = ShutdownAgentAsync(session, cancellationToken).AsTask().WaitAsync(cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }

        await shutdownTask;
    }

    public async ValueTask DisposeAsync() => await _subscriptions.DisposeAsync();

    [LoggerMessage(LogLevel.Information, "Stopping all agents because shutdown signal was received")]
    partial void LogStoppingAllAgentsBecauseShutdownSignalWasReceived();

    [LoggerMessage(LogLevel.Debug,
        "Skipping session {Session}, it is not a root session. It's parent is {ParentSession}, and the root is {RootSession}")]
    partial void LogSkippingNonRootSession(SessionId session, SessionId parentSession, SessionId rootSession);

    [LoggerMessage(LogLevel.Information, "Sending shutdown event to {Session}")]
    partial void LogSendingShutdownEventToId(SessionId session);

    [LoggerMessage(LogLevel.Information, "Shutdown request sent, disposing session {Session}")]
    partial void LogShutdownRequestSentDisposingSessionId(SessionId session);

    [LoggerMessage(LogLevel.Information, "Waiting for all agents to finish shutdown")]
    partial void LogWaitingForAllAgentsToFinishShutdown();

    [LoggerMessage(LogLevel.Information, "Agent shutdown complete")]
    partial void LogAgentShutdownComplete();

    [LoggerMessage(LogLevel.Warning,
        "An agent start request was received for an already started {Session}, but that session was not yet tracked and has been added to tracking")]
    partial void LogAnAgentStartRequestWasReceivedForAnAlreadyStartedSessionButThatSessionWasNotYet(SessionId session);

    [LoggerMessage(LogLevel.Error, "Agent {Session} was already started")]
    partial void LogAgentSessionWasAlreadyStarted(SessionId session);

    [LoggerMessage(LogLevel.Information,
        "Received an agent start request after shutdown, disposing instead of starting")]
    partial void LogReceivedAnAgentStartRequestAfterShutdownDisposingInsteadOfStarting();

    [LoggerMessage(LogLevel.Information, "Attempting to start agent session {Session}")]
    partial void LogAttemptingToStartAgentSessionId(SessionId session);

    [LoggerMessage(LogLevel.Error, "Failed to start agent {Session}")]
    partial void LogFailedToStartAgentId(SessionId session, Exception exception);

    [LoggerMessage(LogLevel.Information, "Agent {Session} started")]
    partial void LogAgentIdStarted(SessionId session);

    [LoggerMessage(LogLevel.Information, "Attempting to stop session {Session}")]
    partial void LogAttemptingToStopSessionSession(SessionId session);

    [LoggerMessage(LogLevel.Error, "Attempted to shutdown failed agent session {Session}, but disposing threw an exception.")]
    partial void LogErrorStateShutdownFault(SessionId session, Exception exception);

    [LoggerMessage(LogLevel.Error, "An exception occurred during agent host shutdown")]
    partial void LogAnExceptionOccurredDuringAgentHostShutdown(Exception exception);
}