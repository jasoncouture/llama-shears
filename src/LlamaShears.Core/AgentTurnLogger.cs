using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core;

public sealed partial class AgentTurnLogger : IEventHandler<ModelTurn>, IDisposable
{
    private readonly ILogger<AgentTurnLogger> _logger;
    private readonly IDisposable _subscription;

    public AgentTurnLogger(IEventBus bus, ILogger<AgentTurnLogger> logger)
    {
        _logger = logger;
        _subscription = bus.Subscribe<ModelTurn>(
            $"{Event.WellKnown.Agent.Turn}:+",
            EventDeliveryMode.Awaited,
            this);
    }

    public ValueTask HandleAsync(IEventEnvelope<ModelTurn> envelope, CancellationToken cancellationToken)
    {
        var agentId = envelope.Type.Id;
        if (string.IsNullOrEmpty(agentId) || envelope.Data is null)
        {
            return ValueTask.CompletedTask;
        }
        LogTurn(_logger, agentId, envelope.Data.Role, envelope.Data.Content);
        return ValueTask.CompletedTask;
    }

    public void Dispose() => _subscription.Dispose();

    [LoggerMessage(Level = LogLevel.Information, Message = "[{AgentId}] [{Role}] {Content}")]
    private static partial void LogTurn(ILogger logger, string agentId, ModelRole role, string content);
}
