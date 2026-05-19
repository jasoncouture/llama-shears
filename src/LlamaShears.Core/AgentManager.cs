using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Seeding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentManager
    : BackgroundService,
      IAgentManager,
      IEventHandler<AgentLoadRequest>,
      IEventHandler<AgentUnloadRequest>,
      IAsyncDisposable
{
    private const string WorkspaceTemplateSubpath = "workspace";

    private readonly IEventPublisher _publisher;
    private readonly ILogger<AgentManager> _logger;
    private readonly IAgentFactory _factory;
    private readonly IShearsPaths _paths;
    private readonly IDirectorySeeder _seeder;

    private readonly Dictionary<string, AgentHandle> _loaded =
        new Dictionary<string, AgentHandle>(StringComparer.OrdinalIgnoreCase);

    private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
    private readonly IDisposable _loadSubscription;
    private readonly IDisposable _unloadSubscription;
    private readonly IDataContextFactory _dataContextFactory;
    private int _disposed;

    public AgentManager(
        IEventBus bus,
        IEventPublisher publisher,
        ILoggerFactory loggerFactory,
        IAgentFactory factory,
        IShearsPaths paths,
        IDirectorySeeder seeder,
        IDataContextFactory dataContextFactory)
    {
        _publisher = publisher;
        _logger = loggerFactory.CreateLogger<AgentManager>();
        _factory = factory;
        _paths = paths;
        _seeder = seeder;
        _dataContextFactory = dataContextFactory;
        _loadSubscription = bus.Subscribe<AgentLoadRequest>(
            $"{Event.WellKnown.Command.AgentLoad}:+",
            EventDeliveryMode.Awaited,
            this);
        _unloadSubscription = bus.Subscribe<AgentUnloadRequest>(
            $"{Event.WellKnown.Command.AgentUnload}:+",
            EventDeliveryMode.Awaited,
            this);
    }

    public IReadOnlyList<string> AgentIds
        => [.. _loaded.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)];

    public bool Contains(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return _loaded.ContainsKey(agentId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var completionSource = new TaskCompletionSource();
        using var cancellationSubscription = stoppingToken.Register(static taskCompletionSource => ((TaskCompletionSource)taskCompletionSource!).TrySetResult(), completionSource);
        await completionSource.Task;
        using var shutdownTimeoutCancellationTokenSource = new CancellationTokenSource();
        shutdownTimeoutCancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
        var tasks = new List<Task>();
        var handles = new List<AgentHandle>();

        foreach (var agentKey in _loaded.Keys.ToArray())
        {
            AgentHandle? handle;
            await _mutex.WaitAsync();
            try
            {
                if (!_loaded.TryGetValue(agentKey, out handle)) continue;
                if (!_loaded.Remove(agentKey)) continue;
            }
            finally
            {
                _mutex.Release();
            }
            handles.Add(handle);
            tasks.Add(PublishStopMessageAsync(handle.Id, shutdownTimeoutCancellationTokenSource.Token)
                .ContinueWith(static async (t, state) =>
                {
                    try
                    {
                        await t;
                    }
                    catch
                    {
                        // Ignored, we're shutting down, don't care.
                    }
                    var localHandle = (AgentHandle)state!;
                    try { 
                    await localHandle.DisposeAsync();
                    } catch
                    {
                        // See previous comment.
                    }
                }, handle)
            );
        }

        await Task.WhenAll(tasks);
    }

    private async Task PublishStopMessageAsync(SessionId id, CancellationToken cancellationToken)
    {
        var stopRequest = new AgentStopRequest(id);
        var eventType = Event.WellKnown.Command.AgentStop with { Id = id };
        await _publisher.PublishAsync(eventType, stopRequest, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _loadSubscription.Dispose();
        _unloadSubscription.Dispose();

        await _mutex.WaitAsync();
        try
        {
            foreach (var name in _loaded.Keys.ToArray())
            {
                await StopAgentAsync(name);
            }
        }
        finally
        {
            _mutex.Release();
        }
        _mutex.Dispose();
    }

    public async ValueTask HandleAsync(IEventEnvelope<AgentLoadRequest> envelope, CancellationToken cancellationToken)
    {
        var name = envelope.Type.Id;
        if (string.IsNullOrWhiteSpace(name) || envelope.Data is null)
        {
            return;
        }
        var config = envelope.Data.Config;

        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (_loaded.TryGetValue(name, out var existing))
            {
                if (string.Equals(existing.ConfigHash, config.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                await ReloadAsync(name, config, cancellationToken);
            }
            else
            {
                await StartAsync(name, config, cancellationToken);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async ValueTask HandleAsync(IEventEnvelope<AgentUnloadRequest> envelope, CancellationToken cancellationToken)
    {
        var name = envelope.Type.Id;
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (await StopAgentAsync(name))
            {
                await _publisher.PublishAsync(
                    Event.WellKnown.Agent.Unloaded with { Id = name },
                    AgentLifecycleMarker.Instance,
                    cancellationToken);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task StartAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        SeedAgentWorkspace(config);

        var handle = await TryStartAgentAsync(config, cancellationToken);
        if (handle is null)
        {
            return;
        }

        _loaded[name] = handle;
        LogAgentStarted(name);

        await _publisher.PublishAsync(
            Event.WellKnown.Agent.Loaded with { Id = name },
            AgentLifecycleMarker.Instance,
            cancellationToken);
    }

    private void SeedAgentWorkspace(AgentConfig config)
    {
        var source = _paths.GetPath(PathKind.Templates, WorkspaceTemplateSubpath);
        var destination = string.IsNullOrWhiteSpace(config.WorkspacePath)
            ? _paths.GetPath(PathKind.Workspace, config.Id)
            : config.WorkspacePath;
        _seeder.SeedIfEmpty(source, destination);
    }

    private async Task ReloadAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        if (_loaded.Remove(name, out var oldHandle))
        {
            await oldHandle.DisposeAsync();
            _dataContextFactory.DeleteContext(name);
        }

        var newHandle = await TryStartAgentAsync(config, cancellationToken);
        if (newHandle is null)
        {
            return;
        }

        _loaded[name] = newHandle;
        LogAgentReloaded(name);
    }

    private async Task<bool> StopAgentAsync(string name)
    {
        if (!_loaded.Remove(name, out var handle))
        {
            return false;
        }
        await handle.DisposeAsync();
        _dataContextFactory.DeleteContext(name);
        LogAgentStopped(handle.Id.Name);
        return true;
    }
    private const string DefaultSessionName = "default";
    private async Task<AgentHandle?> TryStartAgentAsync(AgentConfig config, CancellationToken cancellationToken)
    {
        try
        {
            var sessionId = new SessionId(config.Id, DefaultSessionName);
            return await _factory.StartAgentAsync(config, sessionId, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            LogBuildFailure(config.Id, ex.Message, ex);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Started agent '{AgentId}'.")]
    private partial void LogAgentStarted(string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reloaded agent '{AgentId}'.")]
    private partial void LogAgentReloaded(string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopped agent '{AgentId}'.")]
    private partial void LogAgentStopped(string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}': {Message}")]
    private partial void LogBuildFailure(string agentId, string message, Exception ex);
}
