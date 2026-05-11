namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// Outcome of an <see cref="IAgentConfigProvider.SaveAsync"/> attempt.
/// Pattern-match to handle each branch.
/// </summary>
public abstract record SaveAgentConfigResult
{
    private SaveAgentConfigResult()
    {
    }

    /// <summary>Save succeeded; <paramref name="NewHash"/> is the post-write digest.</summary>
    public sealed record Ok(string NewHash) : SaveAgentConfigResult;

    /// <summary>Expected hash didn't match the on-disk hash; nothing was written.</summary>
    public sealed record Conflict(string CurrentHash) : SaveAgentConfigResult;

    /// <summary>Content failed to deserialize into <see cref="AgentConfig"/>; nothing was written.</summary>
    public sealed record InvalidJson(string Message) : SaveAgentConfigResult;
}
