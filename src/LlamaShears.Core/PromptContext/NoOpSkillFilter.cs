using LlamaShears.Core.Abstractions.PromptContext;

namespace LlamaShears.Core.PromptContext;

public sealed class NoOpSkillFilter : ISkillFilter
{
    public bool AreSkillsAllowed() => true;

    public bool IsSkillAllowed(string skillName) => true;
}
