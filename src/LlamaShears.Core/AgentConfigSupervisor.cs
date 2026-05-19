using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Paths;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace LlamaShears.Core;

public sealed partial class AgentConfigSupervisor : BackgroundService, IEventHandler<SystemTick>
{
    private readonly IEventBus _bus;
    private readonly IAgentConfigProvider _configs;
    private readonly ILogger<AgentConfigSupervisor> _logger;

    private readonly ConcurrentDictionary<string, AgentConfig> _currentConfigs =
        new ConcurrentDictionary<string, AgentConfig>(StringComparer.OrdinalIgnoreCase);

    private long _lastReconcile;
    private int _reconciling;
    private readonly TimeProvider _timeProvider;
    private readonly IApplicationPathProvider _pathProvider;
    private static readonly TimeSpan _reconcileInterval = TimeSpan.FromSeconds(60);

    public AgentConfigSupervisor(
        IEventBus bus,
        IAgentConfigProvider configs,
        TimeProvider timeProvider,
        IApplicationPathProvider pathProvider,
        ILogger<AgentConfigSupervisor> logger)
    {
        _bus = bus;
        _configs = configs;
        _timeProvider = timeProvider;
        _pathProvider = pathProvider;
        _logger = logger;
        _lastReconcile = 0;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var busSubscription = _bus.Subscribe(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);

        var watchPath = _pathProvider.GetPath(PathKind.Agents, ensureExists: true);
        var provider = new PhysicalFileProvider(watchPath);

        // Bridges the sync IChangeToken callback to your async loop without thread starvation
        var changeSignal = new SemaphoreSlim(0, 1);

        var watchRegistration = ChangeToken.OnChange(
            () => provider.Watch("*.json"),
            () =>
            {
                // Don't queue up a billion releases if files are spamming
                if (changeSignal.CurrentCount == 0) changeSignal.Release();
            });

        await using var disposable = busSubscription.And(provider)
            .And(changeSignal)
            .And(watchRegistration);

        // Small delay, so that other stuff has a chance to load and subscribe to stuff
        await Task.Delay(TimeSpan.FromSeconds(0.25), stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Sleeps peacefully until a file changes or the host shuts down
                await changeSignal.WaitAsync(stoppingToken);
                await ForceReconcileAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Host shut down. Let it die gracefully.
        }
        catch (Exception ex)
        {
            LogInitialReconcileFailure(ex);
            throw; // Crash the app, on purpose. it's a bug, it should be painful.
        }
    }

    public async ValueTask HandleAsync(IEventEnvelope<SystemTick>? envelope, CancellationToken cancellationToken)
    {
        if (_lastReconcile != 0 && _timeProvider.GetElapsedTime(_lastReconcile) < _reconcileInterval) return;
        await ForceReconcileAsync(cancellationToken);
    }

    private async ValueTask ForceReconcileAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _reconciling, 1, 0) != 0) return;

        try
        {
            var tasks = new List<Task>();
            await foreach (var change in GetChangeNotificationsAsync(cancellationToken))
            {
                var id = change.UpdatedConfig?.Id ?? change.CurrentConfig?.Id;
                Debug.Assert(id is not null);
                Task task;
                if (change.IsBirth)
                {
                    task = _bus.PublishAsync(
                        Event.WellKnown.Lifecycle.Birth with { Id = id },
                        change.UpdatedConfig!,
                        cancellationToken).AsTask();
                }
                else if (change.IsTombstone)
                {
                    task = _bus.PublishAsync(
                        Event.WellKnown.Lifecycle.Death with { Id = id },
                        AgentDeath.Instance,
                        cancellationToken).AsTask();
                }
                else if (change.IsUpdate)
                {
                    task = _bus.PublishAsync(
                        Event.WellKnown.Lifecycle.Update with { Id = id },
                        change,
                        cancellationToken).AsTask();
                }
                else
                {
                    throw new InvalidOperationException("This code is not reachable, and if it is, that's a bug.");
                }

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            _lastReconcile = _timeProvider.GetTimestamp();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            Interlocked.Exchange(ref _reconciling, 0);
        }
    }

    private async IAsyncEnumerable<ConfigurationChangedNotification> GetChangeNotificationsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var allKeys = _currentConfigs.Keys
            .Union(_configs.ListAgentIds(), StringComparer.OrdinalIgnoreCase)
            .ToImmutableArray();
        foreach (var key in allKeys)
        {
            _currentConfigs.TryGetValue(key, out var current);
            var updated = await _configs.GetConfigAsync(key, cancellationToken);
            if (current?.Hash == updated?.Hash) continue;
            var eventData = new ConfigurationChangedNotification(current, updated);
            if (updated is null) _currentConfigs.TryRemove(key, out _);
            if (updated is not null) _currentConfigs[key] = updated;
            yield return eventData;
        }
    }


    [LoggerMessage(Level = LogLevel.Information, Message = "Published agent-load request for '{AgentId}'.")]
    private partial void LogConfigLoadPublished(string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published agent-unload request for '{AgentId}'.")]
    private partial void LogConfigUnloadPublished(string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Initial agent config reconcile failed.")]
    private partial void LogInitialReconcileFailure(Exception ex);
}