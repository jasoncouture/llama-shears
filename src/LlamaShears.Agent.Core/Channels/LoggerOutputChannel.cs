using LlamaShears.Agent.Abstractions;
using LlamaShears.Provider.Abstractions;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Agent.Core.Channels;

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
