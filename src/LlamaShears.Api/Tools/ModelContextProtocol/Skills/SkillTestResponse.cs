using LlamaShears.Core.Abstractions.PromptContext;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Skills;

public record SkillTestResponse(string? Error, SkillRecord? Skill)
{
    public static implicit operator SkillTestResponse(string errorMessage) => new SkillTestResponse(errorMessage, null);
    public static implicit operator SkillTestResponse(SkillRecord record) => new SkillTestResponse(null, record);
}
