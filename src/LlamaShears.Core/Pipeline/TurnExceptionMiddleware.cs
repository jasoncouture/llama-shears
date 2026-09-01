using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Pipeline;

public sealed partial class TurnExceptionMiddleware : IAgentMiddleware
{
    private readonly ILogger<TurnExceptionMiddleware> _logger;
    private readonly IDataContextScope _dataScope;

    public TurnExceptionMiddleware(
        ILogger<TurnExceptionMiddleware> logger,
        IDataContextScope dataScope)
    {
        _logger = logger;
        _dataScope = dataScope;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        try
        {
            await next.Invoke(context, cancellationToken);
            if (context.Outcome is { Interrupted: true })
            {
                LogTurnInterrupted(_dataScope.GetCurrentSessionId(), context.CorrelationId);
            }
        }
        catch (OperationCanceledException) when (context.TurnToken.IsCancellationRequested
                                                 && !context.ShutdownToken.IsCancellationRequested)
        {
            // Boundary of one turn: interrupt must not kill the loop.
            LogTurnInterrupted(_dataScope.GetCurrentSessionId(), context.CorrelationId);
        }
        catch (OperationCanceledException) when (context.ShutdownToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Boundary of one turn: keep the loop alive so the next
            // inbound batch can run. State is still consistent — the
            // iteration either persisted or it didn't; we do not retry
            // this batch here.
            LogProcessOnceFailed(_dataScope.GetCurrentSessionId(), ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Error,
        Message = "Agent '{Session}' failed to process turn; will retry on next signal.")]
    private partial void LogProcessOnceFailed(SessionId session, Exception ex);

    [LoggerMessage(Level = LogLevel.Information,
        Message =
            "Agent '{Session}' turn '{CorrelationId}' interrupted; partial fragments dropped, agent remains live.")]
    private partial void LogTurnInterrupted(SessionId session, Guid correlationId);
}
