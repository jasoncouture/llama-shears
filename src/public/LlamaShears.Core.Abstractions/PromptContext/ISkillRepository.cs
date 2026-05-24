namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Loads the set of skills visible to a particular agent, applying any configured
/// <see cref="ISkillFilter"/> before the result is returned. Skills are discovered
/// on disk under the host-defined skill roots (global, app, and per-agent workspace
/// tiers) and represented as <see cref="SkillRecord"/> values.
/// </summary>
public interface ISkillRepository
{
    /// <summary>
    /// Returns the skills available to the specified agent after security filters
    /// are applied. The result is an immutable snapshot; callers should treat it
    /// as the authoritative view for the current turn.
    /// </summary>
    /// <param name="agentId">Identifier of the agent whose skills should be enumerated. Used to resolve the per-agent skill root.</param>
    /// <param name="cancellationToken">Cancellation token that aborts the enumeration.</param>
    /// <returns>An <see cref="AgentSkills"/> snapshot containing the filtered set of skills, or <see cref="AgentSkills.None"/> when none are available.</returns>
    ValueTask<AgentSkills> GetSkillsAsync(string agentId, CancellationToken cancellationToken);
}
