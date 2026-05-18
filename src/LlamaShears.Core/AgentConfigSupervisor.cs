using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentConfigSupervisor : IHostedService, IEventHandler<SystemTick>
{
    private readonly IEventBus _bus;
    private readonly IEventPublisher _publisher;
    private readonly IAgentConfigProvider _configs;
    private readonly ILogger<AgentConfigSupervisor> _logger;

    private readonly Dictionary<string, string> _knownHashes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private IDisposable? _subscription;
    private int _reconciling;

    public AgentConfigSupervisor(
        IEventBus bus,
        IEventPublisher publisher,
        IAgentConfigProvider configs,
        ILogger<AgentConfigSupervisor> logger)
    {
        _bus = bus;
        _publisher = publisher;
        _configs = configs;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _bus.Subscribe(
            Event.WellKnown.Host.Tick,
            EventDeliveryMode.FireAndForget,
            this);

        try
        {
            await ReconcileIfIdleAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            LogInitialReconcileFailure(ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        _subscription = null;
        return Task.CompletedTask;
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
            if (!_knownHashes.TryGetValue(name, out var lastHash) ||
                !string.Equals(lastHash, config.Hash, StringComparison.OrdinalIgnoreCase))
            {
                _knownHashes[name] = config.Hash;
                await _publisher.PublishAsync(
                    Event.WellKnown.Command.AgentLoad with { Id = name },
                    new AgentLoadRequest(config),
                    cancellationToken);
                LogConfigLoadPublished(name);
            }
        }

        foreach (var name in _knownHashes.Keys.Where(k => !present.ContainsKey(k)).ToArray())
        {
            _knownHashes.Remove(name);
            await _publisher.PublishAsync(
                Event.WellKnown.Command.AgentUnload with { Id = name },
                AgentUnloadRequest.Instance,
                cancellationToken);
            LogConfigUnloadPublished(name);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Published agent-load request for '{AgentId}'.")]
    private partial void LogConfigLoadPublished(string agentId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Published agent-unload request for '{AgentId}'.")]
    private partial void LogConfigUnloadPublished(string agentId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Initial agent config reconcile failed.")]
    private partial void LogInitialReconcileFailure(Exception ex);
}
