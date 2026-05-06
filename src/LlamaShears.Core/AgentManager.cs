using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.Seeding;
using LlamaShears.Core.Tools.ModelContextProtocol;
using LlamaShears.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentManager : IAgentManager, IHostStartupTask, IEventHandler<SystemTick>, IDisposable
{
    private const string WorkspaceTemplateSubpath = "workspace";

    private readonly IEventBus _bus;
    private readonly IEnumerable<IProviderFactory> _providers;
    private readonly IAgentConfigProvider _configs;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentManager> _logger;
    private readonly IContextStore _contextStore;
    private readonly IServiceProvider _services;
    private readonly IShearsPaths _paths;
    private readonly IDirectorySeeder _seeder;
    private readonly IModelContextProtocolToolDiscovery _toolDiscovery;
    private readonly IModelContextProtocolServerRegistry _serverRegistry;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly Dictionary<string, AgentSlot> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private IDisposable? _subscription;
    private CancellationTokenRegistration _appStartedRegistration;
    private int _reconciling;

    public AgentManager(
        IEventBus bus,
        IEnumerable<IProviderFactory> providers,
        IAgentConfigProvider configs,
        ILoggerFactory loggerFactory,
        IContextStore contextStore,
        IServiceProvider services,
        IShearsPaths paths,
        IDirectorySeeder seeder,
        IModelContextProtocolToolDiscovery toolDiscovery,
        IModelContextProtocolServerRegistry serverRegistry,
        ICurrentAgentAccessor currentAgent,
        IHostApplicationLifetime appLifetime)
    {
        _bus = bus;
        _providers = providers;
        _configs = configs;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AgentManager>();
        _contextStore = contextStore;
        _services = services;
        _paths = paths;
        _seeder = seeder;
        _toolDiscovery = toolDiscovery;
        _serverRegistry = serverRegistry;
        _currentAgent = currentAgent;
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
        // Defer reconciliation until the host has fully started: agent
        // bring-up calls into the in-process MCP listener over loopback,
        // and Kestrel binds at the tail of hosted-service startup. Ticking
        // before that wins us a doomed first-pass discovery.
        _appStartedRegistration = _appLifetime.ApplicationStarted.Register(OnApplicationStarted);
        return ValueTask.CompletedTask;
    }

    private void OnApplicationStarted()
    {
        _subscription = _bus.Subscribe<SystemTick>(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);

        _ = Task.Run(async () =>
        {
            try
            {
                await ReconcileIfIdleAsync(_appLifetime.ApplicationStopping).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogInitialReconcileFailure(_logger, ex);
            }
        });
    }

    public void Dispose()
    {
        _appStartedRegistration.Dispose();
        _subscription?.Dispose();
        _subscription = null;

        foreach (var name in _loaded.Keys.ToArray())
        {
            Stop(name);
        }
    }

    public ValueTask HandleAsync(IEventEnvelope<SystemTick> envelope, CancellationToken cancellationToken)
        => ReconcileIfIdleAsync(cancellationToken);

    private async ValueTask ReconcileIfIdleAsync(CancellationToken cancellationToken)
    {
        // Skip if a previous reconcile is still running. Disk I/O on a
        // 30s tick should never overlap, but a slow filesystem could
        // queue up handlers; collapsing the backlog is correct. The
        // post-startup initial reconcile uses the same gate so a tick
        // that fires mid-bringup doesn't race it.
        if (Interlocked.CompareExchange(ref _reconciling, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await ReconcileAsync(cancellationToken).ConfigureAwait(false);
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
            var config = await _configs.GetConfigAsync(name, cancellationToken).ConfigureAwait(false);
            if (config is not null)
            {
                present[name] = config;
            }
        }

        foreach (var (name, config) in present)
        {
            if (!_loaded.TryGetValue(name, out var slot))
            {
                await StartAsync(name, config, cancellationToken).ConfigureAwait(false);
            }
            else if (slot.Config != config)
            {
                await ReloadAsync(name, config, cancellationToken).ConfigureAwait(false);
            }
        }

        foreach (var name in _loaded.Keys.Where(k => !present.ContainsKey(k)).ToArray())
        {
            Stop(name);
        }
    }

    private async Task StartAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        SeedAgentWorkspace(config);

        var tools = await DiscoverToolsAsync(config, cancellationToken).ConfigureAwait(false);

        var slot = await TryBuildSlotAsync(name, config, tools, cancellationToken).ConfigureAwait(false);
        if (slot is null)
        {
            return;
        }

        _loaded[name] = slot;
        LogAgentStarted(_logger, name);
    }

    private async Task<AgentSlot?> ReloadBuildAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        var tools = await DiscoverToolsAsync(config, cancellationToken).ConfigureAwait(false);
        return await TryBuildSlotAsync(name, config, tools, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<ToolGroup>> DiscoverToolsAsync(
        AgentConfig config,
        CancellationToken cancellationToken)
    {
        var agentInfo = new AgentInfo(
            AgentId: config.Id,
            ModelId: config.Model.Id.Model,
            ContextWindowSize: config.Model.ContextLength ?? 0);

        using var scope = _currentAgent.BeginScope(agentInfo);
        var servers = _serverRegistry.Resolve(config.ModelContextProtocolServers);
        var groups = await _toolDiscovery
            .DiscoverAsync(servers, cancellationToken)
            .ConfigureAwait(false);

        foreach (var group in groups)
        {
            foreach (var tool in group.Tools)
            {
                LogToolDiscovered(_logger, config.Id, group.Source, tool.Name);
            }
        }
        return groups;
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
        var newSlot = await ReloadBuildAsync(name, config, cancellationToken).ConfigureAwait(false);
        if (newSlot is null)
        {
            // Keep the previous slot intact: a build failure shouldn't
            // blow away a working agent.
            return;
        }

        if (_loaded.Remove(name, out var oldSlot))
        {
            oldSlot.Agent.Dispose();
        }

        _loaded[name] = newSlot;
        LogAgentReloaded(_logger, name);
    }

    private void Stop(string name)
    {
        if (_loaded.Remove(name, out var slot))
        {
            slot.Agent.Dispose();
            LogAgentStopped(_logger, slot.Name);
        }
    }

    private async Task<AgentSlot?> TryBuildSlotAsync(
        string name,
        AgentConfig config,
        ImmutableArray<ToolGroup> tools,
        CancellationToken cancellationToken)
    {
        IAgent agent;
        try
        {
            agent = await BuildAgentAsync(name, config, tools, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            LogBuildFailure(_logger, name, ex.Message, ex);
            return null;
        }

        return new AgentSlot(name, agent, config);
    }

    private async Task<IAgent> BuildAgentAsync(
        string name,
        AgentConfig config,
        ImmutableArray<ToolGroup> tools,
        CancellationToken cancellationToken)
    {
        var providerFactory = _providers.FirstOrDefault(p =>
            string.Equals(p.Name, config.Model.Id.Provider, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No provider factory registered with name '{config.Model.Id.Provider}'.");

        var modelConfig = new ModelConfiguration(
            ModelId: config.Model.Id.Model,
            Think: config.Model.Think,
            ContextLength: config.Model.ContextLength,
            KeepAlive: config.Model.KeepAlive,
            TokenLimit: config.Model.TokenLimit);
        var model = providerFactory.CreateModel(modelConfig);

        var agentContext = await _contextStore.OpenAsync(name, cancellationToken).ConfigureAwait(false);

        return ActivatorUtilities.CreateInstance<Agent>(
            _services,
            config,
            model,
            agentContext,
            modelConfig,
            tools);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Started agent '{AgentId}'.")]
    private static partial void LogAgentStarted(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reloaded agent '{AgentId}'.")]
    private static partial void LogAgentReloaded(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopped agent '{AgentId}'.")]
    private static partial void LogAgentStopped(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}': {Message}")]
    private static partial void LogBuildFailure(ILogger logger, string agentId, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Discovered MCP tool '{ToolName}' on server '{ServerName}' for agent '{AgentId}'.")]
    private static partial void LogToolDiscovered(ILogger logger, string agentId, string serverName, string toolName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Initial agent reconcile failed.")]
    private static partial void LogInitialReconcileFailure(ILogger logger, Exception ex);

    private sealed record AgentSlot(string Name, IAgent Agent, AgentConfig Config);
}
