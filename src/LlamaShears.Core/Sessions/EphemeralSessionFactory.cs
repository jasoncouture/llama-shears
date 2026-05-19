using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Sessions;

internal sealed class EphemeralSessionFactory : IEphemeralSessionFactory
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContextStore _contextStore;
    private readonly IAgentConfigProvider _configProvider;
    private readonly TimeProvider _time;
    private readonly ILogger<EphemeralSessionFactory> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public EphemeralSessionFactory(
        IServiceScopeFactory scopeFactory,
        IContextStore contextStore,
        IAgentConfigProvider configProvider,
        TimeProvider time,
        ILogger<EphemeralSessionFactory> logger,
        ILoggerFactory loggerFactory)
    {
        _scopeFactory = scopeFactory;
        _contextStore = contextStore;
        _configProvider = configProvider;
        _time = time;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    public async Task<IEphemeralSession> CreateAsync(
        EphemeralSessionReference parent,
        EphemeralSessionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SystemPromptTemplate);

        var baseConfig = await _configProvider.GetConfigAsync(parent.AgentId, cancellationToken)
            ?? throw new InvalidOperationException($"Agent '{parent.AgentId}' has no loaded configuration.");

        var sessionId = Guid.NewGuid();
        var effectiveChannelId = string.IsNullOrWhiteSpace(request.ChannelId)
            ? $"ephemeral:{sessionId:n}"
            : request.ChannelId;

        var bundle = _scopeFactory.CreateAsyncScopeWithData();
        try
        {
            await bundle.ServiceScope.ApplyScopeDataAsync(cancellationToken);

            var overlaidConfig = baseConfig with { SystemPrompt = request.SystemPromptTemplate };
            var dataScope = bundle.ServiceProvider.GetRequiredService<IDataContextScope>();
            dataScope.SetItem(AgentConfig.DataKey, overlaidConfig);

            var sessionContext = new EphemeralSessionContext
            {
                Parent = parent,
                SessionId = sessionId,
                ChannelId = effectiveChannelId,
            };
            dataScope.SetItem(EphemeralSessionContext.DataKey, sessionContext);

            if (request.TemplateData is { } templateData && templateData.Count > 0)
            {
                dataScope.SetItems(templateData);
            }

            var agentContext = await _contextStore.OpenAsync(parent.AgentId, sessionId, cancellationToken);

            var iterationRunner = bundle.ServiceProvider.GetRequiredService<IAgentIterationRunner>();
            var eventPublisher = bundle.ServiceProvider.GetRequiredService<IEventPublisher>();

            return new EphemeralSession(
                bundle,
                agentContext,
                iterationRunner,
                eventPublisher,
                sessionContext,
                _time,
                _loggerFactory.CreateLogger<EphemeralSession>(),
                request.MaxIterations ?? EphemeralSession.DefaultMaxIterations);
        }
        catch
        {
            await bundle.DisposeAsync();
            throw;
        }
    }
}
