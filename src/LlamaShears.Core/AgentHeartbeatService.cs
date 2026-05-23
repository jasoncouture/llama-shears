using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Events.Channel;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed class AgentHeartbeatService : IAgentService, IEventHandler<SystemTick>, IEventHandler<AgentLifecycleEvent>
{
    private static readonly ActivitySource _activitySource = new ActivitySource("heartbeat", typeof(AgentHeartbeatService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

    public AgentHeartbeatService(IDataContextScope dataScope, ILogger<AgentHeartbeatService> logger, ITransientAgentFactory transientAgentFactory, IContextStore contextStore, TimeProvider timeProvider, IEventBus eventBus, IFileParserCache<AgentHeartbeatService> fileParserCache, IApplicationPathProvider paths)
    {
        _dataScope = dataScope;
        _logger = logger;
        _transientAgentFactory = transientAgentFactory;
        _contextStore = contextStore;
        _timeProvider = timeProvider;
        _eventBus = eventBus;
        _fileParserCache = fileParserCache;
        _paths = paths;
        _isDefaultAgent = _dataScope.GetCurrentSessionId().IsDefault;
        _parentSessionPath = _dataScope.GetSessionPath();
        _parentSession = _parentSessionPath.Current;
        _lastHeartbeatStart = timeProvider.GetLocalNow();
        if (_isDefaultAgent)
        {
            _logger.LogInformation("Agent heartbeat service attached to {SessionPath}", _parentSessionPath);
        }
        else
        {
            _logger.LogDebug("Agent heartbeat service created for {SessionPath}, but this isn't the default session, ignoring", _parentSessionPath);
        }
    }
    private const string HeartbeatPrompt = "HEARTBEAT.md";
    private const string HeartbeatChannelName = "heartbeat";
    private const string HeartbeatTaskFileName = "HEARTBEAT.md";

    private long _lastHeartbeat;
    DateTimeOffset _lastHeartbeatStart;
    private IDisposable? _subscriptions;
    private readonly IDataContextScope _dataScope;
    private readonly ILogger<AgentHeartbeatService> _logger;
    private readonly ITransientAgentFactory _transientAgentFactory;
    private readonly IContextStore _contextStore;
    private readonly TimeProvider _timeProvider;
    private readonly IEventBus _eventBus;
    private readonly IFileParserCache<AgentHeartbeatService> _fileParserCache;
    private readonly IApplicationPathProvider _paths;
    private readonly bool _isDefaultAgent;
    private readonly SessionId _parentSession;
    private readonly SessionPath _parentSessionPath;
    private SessionId? _heartbeatSession;

    public async ValueTask HandleAsync(IEventEnvelope<SystemTick> envelope, CancellationToken cancellationToken)
    {
        var agentConfig = _dataScope.GetAgentConfig();
        var configuredInterval = agentConfig.HeartbeatPeriod;
        if (configuredInterval <= TimeSpan.Zero) return;
        var current = Interlocked.Read(ref _lastHeartbeat);
        var elapsed = _timeProvider.GetElapsedTime(current);
        var updatedTimestamp = _timeProvider.GetTimestamp();
        if (elapsed < configuredInterval) return;
        using var activity = _activitySource.StartActivity("heartbeat", ActivityKind.Internal);

        var heartbeatFile = await ReadHeartbeatFileAsync(cancellationToken);
        if (heartbeatFile is null)
        {
            activity?.SetStatus(ActivityStatusCode.Ok, "No heartbeat file found");
            Interlocked.Exchange(ref _lastHeartbeat, _timeProvider.GetTimestamp());
            return;
        }
        var heartbeatSession = _heartbeatSession;
        if (Interlocked.CompareExchange(ref _lastHeartbeat, updatedTimestamp, current) != current) return;
        using var loggerScope = _logger.BeginScope("{SessionPath}", _parentSessionPath);
        _logger.LogInformation("Starting heartbeat for {SessionPath}", _parentSessionPath);

        if (_heartbeatSession is not null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "Transient agent is already running");
            _logger.LogWarning("Heartbeat interval has elapsed, but a heartbeat session ({Current}) is already active, skipping this beat", heartbeatSession);
            return;
        }

        var heartbeatConfig = agentConfig with
        {
            SystemPrompt = HeartbeatPrompt,
            PromptContext = HeartbeatPrompt
        };
        var contextStorage = await _contextStore.OpenAsync(_parentSession, cancellationToken);

        var heartbeatChildSessionId = new SessionId(agentConfig.Id, HeartbeatChannelName);
        activity?.SetTag("agent.session.current", heartbeatChildSessionId);
        var heartbeatData = new HeartbeatDataContext(heartbeatChildSessionId, _parentSession, heartbeatFile);

        var handle = await _transientAgentFactory.CreateTransientAgent(
            heartbeatConfig,
            HeartbeatChannelName,
            new ModelTurn(ModelRole.User, HeartbeatPrompt, DateTimeOffset.Now, $"subagent:{HeartbeatChannelName}"),
            [new KeyValuePair<string, object?>(HeartbeatDataContext.DataKey, heartbeatData)],
            cancellationToken);

        var heartbeatContextStorage = await _contextStore.OpenAsync(handle.SessionPath.Current, cancellationToken);
        var lastHeartbeatStartedAt = _lastHeartbeatStart;
        _lastHeartbeatStart = _timeProvider.GetLocalNow();
        var parentTurns = contextStorage.Turns
            .Where(i => i.Timestamp > lastHeartbeatStartedAt)
            .ToArray();
        _logger.LogInformation("Summarising {Count} parent turn(s) into briefing for {HeartbeatSessionPath}", parentTurns.Length, handle.SessionPath);
        if (parentTurns.Length > 0)
        {

            var briefing = BuildParentActivityBriefing(parentTurns);
            await heartbeatContextStorage.AppendAsync(briefing, cancellationToken);
            activity?.AddEvent(new ActivityEvent("Added existing context propmpt to context"));
        }
        _heartbeatSession = handle.SessionPath.Current;
        try
        {
            var factory = handle.Scope.ServiceProvider.GetRequiredService<IDataContextFactory>();
            
            _logger.LogInformation("Sending agent start request for transient heartbeat agent session {HeartbeatSessionId}", handle.SessionPath);
            await _eventBus.PublishAsync(
                Event.WellKnown.Command.AgentStart with
                {
                    Id = handle.SessionPath.Current
                },
                new AgentStartRequest(handle),
                cancellationToken);
            _logger.LogInformation("Heartbeat agent {HeartbeatSessionPath} started", handle.SessionPath);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            _logger.LogError(ex, "Failed to start heartbeat agent {HeartbeatSessionPath}", handle.SessionPath);
            await handle.DisposeAsync();
            _heartbeatSession = null;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        if (!_isDefaultAgent) return;
        _logger.LogInformation("Agent heartbeat service starting for agent {ParentSessionPath}", _parentSessionPath);
        _lastHeartbeat = _timeProvider.GetTimestamp();
        var tickSubscription = _eventBus.Subscribe<SystemTick>(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);

        var stoppingSubscipriton = _eventBus.Subscribe<AgentLifecycleEvent>(
            $"{Event.WellKnown.Agent.Stopping}:+",
            EventDeliveryMode.Awaited,
            this);

        _subscriptions = tickSubscription.And(stoppingSubscipriton);
        _logger.LogInformation("Agent heartbeat service started for agent {ParentSessionPath}", _parentSessionPath);
        Activity.Current?.AddEvent(new ActivityEvent("Heartbeat service started"));

    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        if (!_isDefaultAgent) return;
        _logger.LogInformation("Agent heartbeat service stopping for agent: {ParentSessionPath}", _parentSessionPath);
        _subscriptions?.Dispose();
        _subscriptions = null;
        var liveSession = _heartbeatSession;
        try
        {
            if (liveSession is not null)
            {
                await _eventBus.PublishAsync(Event.WellKnown.Command.AgentShutdown, new AgentShutdownRequest(liveSession), cancellationToken);
            }
            _logger.LogInformation("Agent heartbeat service stopped for agent: {ParentSessionPath}", _parentSessionPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured trying to stop running heartbeat agent: {HeartbeatSessionPath}", liveSession);
        }
        finally
        {
            Interlocked.Exchange(ref _heartbeatSession, null);
            Interlocked.Exchange(ref _lastHeartbeat, long.MaxValue);
        }
        Activity.Current?.AddEvent(new ActivityEvent("Heartbeat service stopped"));
    }

    public ValueTask HandleAsync(IEventEnvelope<AgentLifecycleEvent> envelope, CancellationToken cancellationToken)
    {
        if (_heartbeatSession is null) return ValueTask.CompletedTask;
        if (envelope.Data?.SessionId != _heartbeatSession) return ValueTask.CompletedTask;
        Interlocked.Exchange(ref _lastHeartbeat, _timeProvider.GetTimestamp());
        Interlocked.Exchange(ref _heartbeatSession, null);
        return ValueTask.CompletedTask;
    }

    private ModelTurn BuildParentActivityBriefing(IReadOnlyList<ModelTurn> parentTurns)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine(System.Text.Json.JsonSerializer.Serialize(new
        {
            kind = "parent_session_activity",
            since = _lastHeartbeatStart,
            note = "Recent activity in the parent session you are monitoring. You did NOT author or receive any of these turns. They are reference material so you can spot pending work.",
            count = parentTurns.Count,
        }));
        foreach (var turn in parentTurns)
        {
            builder.AppendLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                role = turn.Role.ToString(),
                timestamp = turn.Timestamp,
                channel = turn.ChannelId,
                content = turn.Content,
            }));
        }
        return new ModelTurn(
            ModelRole.SystemEphemeral,
            builder.ToString(),
            _timeProvider.GetLocalNow());
    }

    private async ValueTask<WorkspaceFile?> ReadHeartbeatFileAsync(CancellationToken cancellationToken)
    {
        var fullPath = _paths.GetPath(PathKind.Workspace, Path.Combine(_parentSession.AgentId, HeartbeatTaskFileName));
        return await _fileParserCache.GetOrParseAsync(fullPath, fullPath, ParseHeartbeatFileAsync, cancellationToken);
    }

    private static async ValueTask<WorkspaceFile?> ParseHeartbeatFileAsync(Stream? stream, string fullPath, CancellationToken cancellationToken)
    {
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var directory = Path.GetDirectoryName(fullPath) ?? string.Empty;
        if (directory.Length > 0 && directory[^1] != Path.DirectorySeparatorChar)
        {
            directory += Path.DirectorySeparatorChar;
        }
        return new WorkspaceFile(Path.GetFileName(fullPath), directory, content);
    }
}