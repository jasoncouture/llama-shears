namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Spawns a one-shot transient child of the ambient root session
/// (<c>subagent_run</c>). Nested children are refused.
/// </summary>
public interface ISubagentRunner
{
    /// <summary>
    /// Starts a child session for <paramref name="request"/>. When
    /// <see cref="SubagentRunRequest.AwaitResult"/> is
    /// <see langword="true"/>, waits for the child to idle (or time
    /// out) and returns the last assistant text. When
    /// <see langword="false"/>, returns as soon as the child is
    /// started; the child reports later via the parent channel or
    /// <c>session_send</c>.
    /// </summary>
    ValueTask<SubagentRunResult> RunAsync(SubagentRunRequest request, CancellationToken cancellationToken);
}
