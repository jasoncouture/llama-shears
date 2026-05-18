namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Caller-owned handle to a live ephemeral session. Wraps a private
/// agent-style loop driven by the same iteration runner the main agent
/// uses, persists turns under
/// <c>&lt;agentId&gt;/&lt;sessionGuid&gt;/current.json</c>, and emits a
/// reply back to the parent session (via the event bus) on completion.
/// Owned by the caller — disposing tears down the session's scope but
/// leaves the on-disk transcript intact.
/// </summary>
public interface IEphemeralSession : IAsyncDisposable
{
    /// <summary>
    /// Identity of this ephemeral session — the (agentId, sessionId)
    /// pair under which its transcript persists. <c>SessionId</c> is
    /// never null on an ephemeral session.
    /// </summary>
    EphemeralSessionReference Reference { get; }

    /// <summary>
    /// Drives the session's private iteration loop against
    /// <paramref name="initialPrompt"/>, persisting all turns and tool
    /// results into the session's context as it goes, until the model
    /// produces an assistant turn with no tool calls, the iteration cap
    /// is reached, or <paramref name="cancellationToken"/> trips.
    /// </summary>
    Task<EphemeralRunResult> RunAsync(string initialPrompt, CancellationToken cancellationToken);
}
