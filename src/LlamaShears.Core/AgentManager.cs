using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Seeding;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IShearsPaths _paths;
    private readonly IDirectorySeeder _seeder;

    private readonly Dictionary<string, AgentSlot> _loaded =
        new Dictionary<string, AgentSlot>(StringComparer.OrdinalIgnoreCase);

    private readonly SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);
    private readonly IDisposable _loadSubscription;
    private readonly IDisposable _unloadSubscription;
    private readonly IDataContextFactory _dataContextFactory;
    private int _disposed;

    public AgentManager(
        IEventBus bus,
        IEventPublisher publisher,
        ILoggerFactory loggerFactory,
        IServiceScopeFactory scopeFactory,
        IShearsPaths paths,
        IDirectorySeeder seeder,
        IDataContextFactory dataContextFactory)
    {
        _publisher = publisher;
        _logger = loggerFactory.CreateLogger<AgentManager>();
        _scopeFactory = scopeFactory;
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

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Task.Delay(Timeout.Infinite, stoppingToken);

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
            await oldSlot.DisposeAsync();
            _dataContextFactory.DeleteContext(name);
        }

        var newSlot = await TryBuildSlotAsync(name, config, cancellationToken);
        if (newSlot is null)
        {
            return;
        }

        _loaded[name] = newSlot;
        LogAgentReloaded(name);
    }

    private async Task<bool> StopAgentAsync(string name)
    {
        if (!_loaded.Remove(name, out var slot))
        {
            return false;
        }
        await slot.DisposeAsync();
        _dataContextFactory.DeleteContext(name);
        LogAgentStopped(slot.Name);
        return true;
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
                // Resolve the language model eagerly so a missing/misconfigured provider surfaces here as an
                // InvalidOperationException and TryBuildSlotAsync skips the agent — instead of failing mid-turn.
                _ = scope.ServiceProvider.GetRequiredService<ILanguageModel>();
                var agent = scope.ServiceProvider.GetRequiredService<IAgent>();
                await agent.StartAsync(cancellationToken);

                return new AgentSlot(name, config.Hash, scope);
            }
            catch
            {
                await scope.DisposeAsync();
                throw;
            }
        }
        finally
        {
            ExecutionContext.Restore(previousContext!);
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

    private sealed record AgentSlot(string Name, string ConfigHash, AsyncServiceScope Scope) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Scope.DisposeAsync();
        }
    }
}
