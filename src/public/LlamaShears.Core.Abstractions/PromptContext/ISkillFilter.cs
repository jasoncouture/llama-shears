namespace LlamaShears.Core.Abstractions.PromptContext;

/// <summary>
/// Gatekeeps skill availability for the current agent context. The repository
/// consults the filter before enumerating skills (<see cref="AreSkillsAllowed"/>)
/// and again per skill (<see cref="IsSkillAllowed"/>) so policies can be applied
/// globally or per name.
/// </summary>
public interface ISkillFilter
{
    /// <summary>
    /// Indicates whether skills are enabled at all for the current context.
    /// Returning <see langword="false"/> short-circuits enumeration and yields an empty result.
    /// </summary>
    /// <returns><see langword="true"/> when skills should be exposed; otherwise <see langword="false"/>.</returns>
    bool AreSkillsAllowed();

    /// <summary>
    /// Indicates whether a specific skill is permitted in the current context.
    /// Invoked once per discovered skill after <see cref="AreSkillsAllowed"/> has returned <see langword="true"/>.
    /// </summary>
    /// <param name="skillName">Name of the skill as declared in its <c>SKILL.md</c> frontmatter.</param>
    /// <returns><see langword="true"/> when the named skill should be exposed; otherwise <see langword="false"/>.</returns>
    bool IsSkillAllowed(string skillName);
}
