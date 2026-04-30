using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Channels;
using LlamaShears.Hosting;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentManager : IHostStartupTask, IDisposable
{
    private readonly IAsyncSubscriber<SystemTick> _ticks;
    private readonly IEnumerable<IProviderFactory> _providers;
    private readonly IAgentConfigProvider _configs;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentManager> _logger;
    private readonly IContextStore _contextStore;
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, AgentSlot> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private IDisposable? _subscription;
    private int _reconciling;

    public AgentManager(
        IAsyncSubscriber<SystemTick> ticks,
        IEnumerable<IProviderFactory> providers,
        IAgentConfigProvider configs,
        ILoggerFactory loggerFactory,
        IContextStore contextStore,
        IServiceProvider services)
    {
        _ticks = ticks;
        _providers = providers;
        _configs = configs;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AgentManager>();
        _contextStore = contextStore;
        _services = services;
    }

    public IReadOnlyDictionary<string, IAgent> Agents
        => _loaded.ToDictionary(kv => kv.Key, kv => kv.Value.Agent, StringComparer.OrdinalIgnoreCase);

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _ticks.Subscribe(OnTickAsync);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _subscription = null;

        foreach (var name in _loaded.Keys.ToArray())
        {
            Stop(name);
        }
    }

    private async ValueTask OnTickAsync(SystemTick tick, CancellationToken cancellationToken)
    {
        // Skip if a previous tick is still reconciling. Disk I/O on a
        // 30s tick should never overlap, but a slow filesystem could
        // queue up handlers; collapsing the backlog is correct.
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
            var config = _configs.GetConfig(name);
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
        var slot = await TryBuildSlotAsync(name, config, cancellationToken).ConfigureAwait(false);
        if (slot is null)
        {
            return;
        }

        _loaded[name] = slot;
        LogAgentStarted(_logger, name);
    }

    private async Task ReloadAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        var newSlot = await TryBuildSlotAsync(name, config, cancellationToken).ConfigureAwait(false);
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

    private async Task<AgentSlot?> TryBuildSlotAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        IAgent agent;
        try
        {
            agent = await BuildAgentAsync(name, config, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            LogBuildFailure(_logger, name, ex.Message, ex);
            return null;
        }

        return new AgentSlot(name, agent, config);
    }

    private async Task<IAgent> BuildAgentAsync(string name, AgentConfig config, CancellationToken cancellationToken)
    {
        var providerFactory = _providers.FirstOrDefault(p =>
            string.Equals(p.Name, config.Model.Id.Provider, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No provider factory registered with name '{config.Model.Id.Provider}'.");

        var model = providerFactory.CreateModel(new ModelConfiguration(
            ModelId: config.Model.Id.Model,
            Think: config.Model.Think,
            ContextLength: config.Model.ContextLength,
            KeepAlive: config.Model.KeepAlive));

        var agentContext = await _contextStore.OpenAsync(name, cancellationToken).ConfigureAwait(false);

        IReadOnlyList<IInputChannel> inputs =
        [
            ActivatorUtilities.CreateInstance<UiInputChannel>(_services, name),
        ];

        IReadOnlyList<IOutputChannel> outputs =
        [
            ActivatorUtilities.CreateInstance<UiOutputChannel>(_services, name),
            ActivatorUtilities.CreateInstance<LoggerOutputChannel>(_services, name),
        ];

        var logger = Agent.CreateLogger(_loggerFactory, name);

        return ActivatorUtilities.CreateInstance<Agent>(
            _services,
            name,
            config,
            model,
            agentContext,
            inputs,
            outputs,
            logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Started agent '{AgentId}'.")]
    private static partial void LogAgentStarted(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reloaded agent '{AgentId}'.")]
    private static partial void LogAgentReloaded(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopped agent '{AgentId}'.")]
    private static partial void LogAgentStopped(ILogger logger, string agentId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping agent '{AgentId}': {Message}")]
    private static partial void LogBuildFailure(ILogger logger, string agentId, string message, Exception ex);

    private sealed record AgentSlot(string Name, IAgent Agent, AgentConfig Config);
}
