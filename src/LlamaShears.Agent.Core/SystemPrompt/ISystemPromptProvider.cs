namespace LlamaShears.Agent.Core.SystemPrompt;

/// <summary>
/// Builds the system-prompt body that seeds an agent's context. The
/// current implementation is a hard-coded prompt with the supplied
/// timestamp appended; this interface exists as the seam the future
/// template/file-based prompt builder will replace without touching the
/// agent-construction path.
/// </summary>
public interface ISystemPromptProvider
{
    /// <summary>
    /// Produces the system-prompt body for <paramref name="agentId"/>.
    /// Implementations must include <paramref name="now"/> verbatim
    /// somewhere in the output so the agent has an authoritative
    /// reference to wall-clock time at construction.
    /// </summary>
    string Build(string agentId, DateTimeOffset now);
}
