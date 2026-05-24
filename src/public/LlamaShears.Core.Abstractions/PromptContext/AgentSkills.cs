using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Immutable snapshot of the skills exposed to a single agent for a single turn.
/// Produced by <see cref="ISkillRepository.GetSkillsAsync"/> after filtering, and
/// surfaced through the prompt-context data bag under <see cref="DataKey"/>.
/// </summary>
/// <param name="Skills">The filtered set of skills available to the agent.</param>
public record AgentSkills(ImmutableArray<SkillRecord> Skills)
{
    /// <summary>An empty snapshot returned when skills are disabled or none are visible.</summary>
    public static AgentSkills None { get; } = new AgentSkills([]);

    /// <summary>The well-known key used to publish this snapshot into the prompt-context data bag.</summary>
    public const string DataKey = "skill_info";

    /// <summary>Indicates whether at least one skill is present in the snapshot.</summary>
    public bool Available => !Skills.IsDefaultOrEmpty;
}
