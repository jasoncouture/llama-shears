namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Creates ephemeral sessions for a loaded agent. The new session gets a
/// fresh <see cref="Guid"/> id, its own service scope, its own data
/// context overlay (so prompt-template parameters and the parent
/// reference don't leak back to the main agent), and an empty transcript
/// — no parent turns are inherited.
/// </summary>
public interface IEphemeralSessionFactory
{
    /// <summary>
    /// Creates and returns a session targeting <paramref name="parent"/>
    /// for its <c>session_reply</c> output. The session is not started
    /// until the caller invokes <see cref="IEphemeralSession.RunAsync"/>;
    /// the caller owns disposal.
    /// </summary>
    Task<IEphemeralSession> CreateAsync(
        EphemeralSessionReference parent,
        EphemeralSessionRequest request,
        CancellationToken cancellationToken);
}
