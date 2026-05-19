using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Seeding;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentLifecycleService : 
    BackgroundService,
    IEventHandler<AgentConfig>,
    IEventHandler<AgentDeath>
{
    private const string WorkspaceTemplateSubpath = "workspace";
    private const string DefaultSessionName = "default";

    private readonly IEventBus _bus;
    private readonly IAgentFactory _factory;
    private readonly IAgentInstanceRepository _repository;
    private readonly IApplicationPathProvider _paths;
    private readonly IDirectorySeeder _seeder;
    private readonly ILogger<AgentLifecycleService> _logger;

    public AgentLifecycleService(
        IEventBus bus,
        IAgentFactory factory,
        IAgentInstanceRepository repository,
        IApplicationPathProvider paths,
        IDirectorySeeder seeder,
        ILogger<AgentLifecycleService> logger)
    {
        _bus = bus;
        _factory = factory;
        _repository = repository;
        _paths = paths;
        _seeder = seeder;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var birthSubscription = _bus.Subscribe<AgentConfig>(
            $"{Event.WellKnown.Lifecycle.Birth}:+",
            EventDeliveryMode.Awaited,
            this);
        var deathSubscription = _bus.Subscribe<AgentDeath>(
            $"{Event.WellKnown.Lifecycle.Death}:+",
            EventDeliveryMode.Awaited,
            this);

        var taskCompletionSource = new TaskCompletionSource();
        var stopRegistration = stoppingToken.Register(
            static s => ((TaskCompletionSource)s!).TrySetResult(),
            taskCompletionSource);

        await using var disposable = birthSubscription
            .And(deathSubscription)
            .And(stopRegistration as IAsyncDisposable);

        await taskCompletionSource.Task;
    }

    public async ValueTask HandleAsync(IEventEnvelope<AgentConfig> envelope, CancellationToken cancellationToken)
    {
        if (envelope.Data is not { } config) return;

        SeedAgentWorkspace(config);

        AgentHandle? handle = null;
        try
        {
            var sessionId = new SessionId(config.Id, DefaultSessionName);
            handle = await _factory.CreateAgentAsync(
                config,
                new SessionPath(sessionId),
                [],
                cancellationToken);

            await _bus.PublishAsync(
                Event.WellKnown.Command.AgentStart with { Id = config.Id },
                new AgentStartRequest(handle),
                cancellationToken);
        }
        catch (Exception ex)
        {
            LogBirthFailure(config.Id, ex.Message, ex);
            if (handle is not null)
            {
                try
                {
                    await handle.DisposeAsync();
                }
                catch (Exception disposeException)
                {
                    LogBirthCleanupFailure(config.Id, disposeException);
                }
            }
        }
    }

    public async ValueTask HandleAsync(IEventEnvelope<AgentDeath> envelope, CancellationToken cancellationToken)
    {
        var agentId = envelope.Type.Id;
        if (string.IsNullOrWhiteSpace(agentId)) return;

        var root = _repository.GetAllAgents()
            .FirstOrDefault(handle =>
                handle.SessionPath.IsRootSession &&
                string.Equals(handle.SessionPath.Current.AgentId, agentId, StringComparison.OrdinalIgnoreCase));

        if (root is null)
        {
            LogDeathMiss(agentId);
            return;
        }

        await _bus.PublishAsync(
            Event.WellKnown.Command.AgentStop with { Id = agentId },
            new AgentStopRequest(root.SessionPath.Current),
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

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to birth agent '{AgentId}': {Message}")]
    private partial void LogBirthFailure(string agentId, string message, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to dispose orphaned handle for '{AgentId}' after birth failure.")]
    private partial void LogBirthCleanupFailure(string agentId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Received death event for '{AgentId}', but no root session was found in the repository")]
    private partial void LogDeathMiss(string agentId);
}
