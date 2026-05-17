using System.Collections.Immutable;
using System.Text.RegularExpressions;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Seeding;
using LlamaShears.Core.Tools.ModelContextProtocol;
using LlamaShears.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentManager : IAgentManager, IHostStartupTask, IEventHandler<SystemTick>, IAsyncDisposable
{
    private const string WorkspaceTemplateSubpath = "workspace";

    private readonly IEventBus _bus;
    private readonly IEventPublisher _publisher;
    private readonly IAgentConfigProvider _configs;
    private readonly ILogger<AgentManager> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IShearsPaths _paths;
    private readonly IDirectorySeeder _seeder;
    private readonly IHostApplicationLifetime _appLifetime;

    private readonly Dictionary<string, AgentSlot> _loaded =
        new Dictionary<string, AgentSlot>(StringComparer.OrdinalIgnoreCase);

    private IDisposable? _subscription;
    private CancellationTokenRegistration _appStartedRegistration;
    private int _reconciling;
    private readonly IDataContextFactory _dataContextFactory;

    public AgentManager(
        IEventBus bus,
        IEventPublisher publisher,
        IAgentConfigProvider configs,
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IShearsPaths paths,
        IDirectorySeeder seeder,
        IDataContextFactory dataContextFactory,
        IHostApplicationLifetime appLifetime)
    {
        _bus = bus;
        _publisher = publisher;
        _configs = configs;
        _logger = loggerFactory.CreateLogger<AgentManager>();
        _scopeFactory = scopeFactory;
        _paths = paths;
        _seeder = seeder;
        _dataContextFactory = dataContextFactory;
        _appLifetime = appLifetime;
    }

    public IReadOnlyList<string> AgentIds
        => [.. _loaded.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)];

    public bool Contains(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return _loaded.ContainsKey(agentId);
    }

    public IAgent? Get(string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return _loaded.TryGetValue(agentId, out var slot) ? slot.Agent : null;
    }

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        _appStartedRegistration = _appLifetime.ApplicationStarted.Register(OnApplicationStarted);
        return ValueTask.CompletedTask;
    }

    private void OnApplicationStarted()
    {
        _subscription = _bus.Subscribe(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);

        _ = Task.Run(async () =>
        {
            try
            {
                await ReconcileIfIdleAsync(_appLifetime.ApplicationStopping);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogInitialReconcileFailure(ex);
            }
        });
    }

    public async ValueTask DisposeAsync()
    {
        _appStartedRegistration.Dispose();
        _subscription?.Dispose();
        _subscription = null;

        foreach (var name in _loaded.Keys.ToArray())
        {
            await StopAsync(name);
        }
    }

    public ValueTask HandleAsync(IEventEnvelope<SystemTick> envelope, CancellationToken cancellationToken)
        => ReconcileIfIdleAsync(cancellationToken);

    private async ValueTask ReconcileIfIdleAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.CompareExchange(ref _reconciling, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await ReconcileAsync(cancellationToken);
        }
        finally
        {
            Interlocked.Exchange(ref _reconciling, 0);
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        var present = new Dictionary<string, AgentConfig>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in _configs.ListAgentIds())
        {
            var config = await _configs.GetConfigAsync(name, cancellationToken);
            if (config is not null)
            {
                present[name] = config;
            }
        }

        foreach (var (name, config) in present)
        {
            if (!_loaded.TryGetValue(name, out var slot))
            {
                await StartAsync(name, config, cancellationToken);
            }
            else if (!string.Equals(slot.Config.Hash, config.Hash, StringComparison.OrdinalIgnoreCase))
            {
                await ReloadAsync(name, config, cancellationToken);
            }
        }

        foreach (var name in _loaded.Keys.Where(k => !present.ContainsKey(k)).ToArray())
        {
            await StopAsync(name);
        }
    }

    private async Task StartAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        SeedAgentWorkspace(config);

        var slot = await TryBuildSlotAsync(name, config, cancellationToken);
        if (slot is null)
        {
            return;
        }

        _loaded[name] = slot;
        LogAgentStarted(name);

        await _publisher.PublishAsync(
            Event.WellKnown.Agent.Loaded with { Id = name },
            AgentLifecycleMarker.Instance,
            cancellationToken);
    }

    private Task<AgentSlot?> ReloadBuildAsync(string name, AgentConfig config,
        CancellationToken cancellationToken)
        => TryBuildSlotAsync(name, config, cancellationToken);

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
        if (_loaded.Remove(name, out var oldSlot))
        {
            await DisposeSlotAsync(oldSlot);
            _dataContextFactory.DeleteContext(name);
        }

        var newSlot = await ReloadBuildAsync(name, config, cancellationToken);
        if (newSlot is null)
        {
            return;
        }

        _loaded[name] = newSlot;
        LogAgentReloaded(name);
    }

    private async Task StopAsync(string name)
    {
        if (_loaded.Remove(name, out var slot))
        {
            await DisposeSlotAsync(slot);
            _dataContextFactory.DeleteContext(name);
            LogAgentStopped(slot.Name);
        }
    }

    private async Task<AgentSlot?> TryBuildSlotAsync(
        string name,
        AgentConfig config,
        CancellationToken cancellationToken)
    {
        try
        {
            return await BuildSlotAsync(name, config, cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            LogBuildFailure(name, ex.Message, ex);
            return null;
        }
    }

    private async Task<AgentSlot> BuildSlotAsync(
        string name,
        AgentConfig config,
        CancellationToken cancellationToken)
    {
        var previousContext = ExecutionContext.Capture();
        try
        {
            var uiCulture = Thread.CurrentThread.CurrentUICulture;
            var culture = Thread.CurrentThread.CurrentCulture;
            var blankExecutionContext = await ExecutionState.CreateBlankContextAsync();

            ExecutionContext.Restore(blankExecutionContext);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = uiCulture;
            // And we now look like we are a fresh process or thread!

            var modelConfig = config.Model;
            
            var agentGlobalDataContext = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    { AgentConfig.DataKey, config },
                    { ModelConfiguration.DataKey, modelConfig },
                };

            var scope = _scopeFactory.CreateAsyncScope();
            try
            {
                var dataContextFactory = scope.ServiceProvider.GetRequiredService<IDataContextFactory>();
                dataContextFactory.CreateContext(config.Id);
                var dataProviders = scope.ServiceProvider.GetScopedDataProviders();
                await dataContextFactory.InitializeAsync(config.Id, dataProviders, agentGlobalDataContext,
                    cancellationToken);
                _ = scope.ServiceProvider.GetRequiredService<ILanguageModel>();
                _ = scope.ServiceProvider.GetRequiredService<CompactionAgentService>();
                var agent = scope.ServiceProvider.GetRequiredService<IAgent>();
                await agent.StartAsync(cancellationToken);

                return new AgentSlot(name, agent, config, scope);
            }
            catch
            {
                await scope.DisposeAsync();
                throw;
            }
        }
        finally
        {
            // We don't need to worry about cleaning up after ourselves, the data context lives within the execution
            // context as an async local, once all are gone, the weak reference gets GC'd and it gets cleaned up.
            // We're going back to our caller, with the state they called us in.
            ExecutionContext.Restore(previousContext!);
        }
    }

    private static ValueTask DisposeSlotAsync(AgentSlot slot)
    {
        // Agent is registered scoped, so the scope tracks it and the
        // scope's disposal cascades to the agent's IAsyncDisposable.
        // The manager owns the scope, not the agent directly.
        return slot.Scope.DisposeAsync();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Started agent '{AgentId}'.")]
    private partial void LogAgentStarted(string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reloaded agent '{AgentId}'.")]
    private partial void LogAgentReloaded(string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopped agent '{AgentId}'.")]
    private partial void LogAgentStopped(string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}': {Message}")]
    private partial void LogBuildFailure(string agentId, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Initial agent reconcile failed.")]
    private partial void LogInitialReconcileFailure(Exception ex);

    private sealed record AgentSlot(string Name, IAgent Agent, AgentConfig Config, AsyncServiceScope Scope);
}