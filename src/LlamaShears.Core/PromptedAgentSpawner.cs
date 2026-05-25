using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using LlamaShears.Core.Abstractions;
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

public class PromptedAgentSpawner : IPromptedAgentSpawner
{
    private static readonly ActivitySource _activitySource = Telemetry.CreateActivitySourceForType<PromptedAgentSpawner>();
    private readonly IContextStore _contextStore;
    private readonly ILogger<PromptedAgentSpawner> _logger;
    private readonly ITransientAgentFactory _transientAgentFactory;
    private readonly IEventBus _eventBus;

    public PromptedAgentSpawner(IContextStore contextStore, ILogger<PromptedAgentSpawner> logger, ITransientAgentFactory transientAgentFactory, IEventBus eventBus)
    {
        _contextStore = contextStore;
        _logger = logger;
        _transientAgentFactory = transientAgentFactory;
        _eventBus = eventBus;
    }

    public async ValueTask<AgentHandle> CreateAsync(
        PromptedAgentStartInformation startInfo,
        CancellationToken cancellationToken
        )
    {
        if (!startInfo.ParentSessionPath.IsRootSession) throw new ArgumentException("Parent session must be a root session, nested sub-agents are not allowed.", nameof(startInfo));
        if (startInfo.InitialPrompt is not { Role: ModelRole.User }) throw new ArgumentException("The initial prompt must be a model role of user", nameof(startInfo));
        if (startInfo.Id.IsDefault) throw new InvalidOperationException("A sub agent cannot be the default session");
        if (startInfo.Id.AgentId != startInfo.ParentSessionPath.Current.AgentId) throw new InvalidOperationException("The agent ID must match the parent session agent ID");

        using var activity = _activitySource.StartActivity($"session spawn {startInfo.Id.Name}", ActivityKind.Client);
        activity?.SetTag("agent.session.name", startInfo.Id!.Name);
        activity?.SetTag("agent.session.id", startInfo.Id);
        activity?.SetTag("agent.session.parent.id", startInfo.ParentSessionPath);

        startInfo = startInfo.WithMissingPropertiesGenerated();

        var handle = await _transientAgentFactory.CreateTransientAgent(
            startInfo.Config,
            startInfo.Id.Name,
            startInfo.InitialPrompt with { ChannelId = $"subagent:{startInfo.Id.Name}" },
            startInfo.ContextData!,
            cancellationToken);

        var subAgentContextStorage = await _contextStore.OpenAsync(handle.SessionPath.Current, cancellationToken);
        foreach (var turn in startInfo.Turns!)
        {
            await subAgentContextStorage.AppendAsync(turn, cancellationToken);
        }

        if (!startInfo.AutoStart)
        {
            activity?.SetStatus(ActivityStatusCode.Ok, "Agent created");
            return handle;
        }

        try
        {
            if (startInfo.AutoStart)
            {
                _logger.LogInformation("Sending agent start request for transient {Name} agent session {Session}", startInfo.Id.Name, handle.SessionPath);
                await _eventBus.PublishAsync(
                    Event.WellKnown.Command.AgentStart with
                    {
                        Id = handle.SessionPath.Current
                    },
                    new AgentStartRequest(handle),
                    cancellationToken);
                _logger.LogInformation("Heartbeat agent {Session} started", handle.SessionPath);
                activity?.SetStatus(ActivityStatusCode.Ok, "Agent started");
            }

            return handle;
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            activity?.SetStatus(ActivityStatusCode.Error);
            _logger.LogError(ex, "Failed to start agent {Session}", handle.SessionPath);
            await handle.DisposeAsync();
            throw;
        }
    }
}