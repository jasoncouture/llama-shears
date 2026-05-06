using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Channels;

public sealed class LoggerOutputChannel : IOutputChannel
{
    private readonly ILogger<LoggerOutputChannel> _logger;
    private readonly string _agentId;

    public LoggerOutputChannel(ILogger<LoggerOutputChannel> logger, string agentId)
    {
        _logger = logger;
        _agentId = agentId;
    }

    public Task SendAsync(ModelTurn turn, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[{AgentId}] [{Role}] {Content}",
            _agentId,
            turn.Role,
            turn.Content);
        return Task.CompletedTask;
    }
}
