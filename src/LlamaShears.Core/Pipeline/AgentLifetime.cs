using LlamaShears.Core.Abstractions.Agent.Pipeline;

namespace LlamaShears.Core.Pipeline;

public sealed class AgentLifetime : IAgentLifetime
{
    private readonly CancellationTokenSource _stopping = new();

    /// <inheritdoc />
    public CancellationToken Stopping => _stopping.Token;

    /// <inheritdoc />
    public void Stop()
    {
        // Cancel, do not dispose: the loop still reads Stopping after
        // Stop, and DI may dispose this instance while the loop is
        // joining. A disposed token would throw on those reads.
        try
        {
            _stopping.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down; the loop is gone or going.
        }
    }
}
