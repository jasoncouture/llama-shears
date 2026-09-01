namespace LlamaShears.Core.Abstractions.Agent.Pipeline;

/// <summary>
/// Per-agent stop signal, analogous to
/// <c>IHostApplicationLifetime</c> for one agent scope. The loop
/// watches <see cref="Stopping"/>; inbound shutdown handling and
/// dispose call <see cref="Stop"/>.
/// </summary>
public interface IAgentLifetime
{
    /// <summary>
    /// Trips when <see cref="Stop"/> has been called. The loop uses
    /// this as its run-loop cancellation token.
    /// </summary>
    CancellationToken Stopping { get; }

    /// <summary>
    /// Requests that the agent loop exit. Idempotent. Does not wait
    /// for the loop to finish — callers that need to join do that
    /// themselves.
    /// </summary>
    void Stop();
}
