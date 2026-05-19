namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Outcome of an ephemeral session's <c>RunAsync</c> call.
/// </summary>
/// <param name="ReplySent">
/// <see langword="true"/> when at least one reply was delivered to the
/// parent session — either by the session calling <c>session_reply</c>
/// at least once, or by the fallback path emitting the last assistant
/// content turn on its behalf.
/// </param>
/// <param name="UsedFallback">
/// <see langword="true"/> when the reply was produced by the fallback
/// path because <c>session_reply</c> was never called during the session.
/// Mutually exclusive with the explicit-tool path.
/// </param>
/// <param name="Iterations">
/// Number of iteration cycles the session loop completed before exiting.
/// </param>
public sealed record EphemeralRunResult(
    bool ReplySent,
    bool UsedFallback,
    int Iterations);
