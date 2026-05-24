using LlamaShears.Core.Abstractions.PromptContext;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Skills;

public record GetSkillResponse(string? Error, SkillRecord? Skill)
{
    public static implicit operator GetSkillResponse(string errorMessage) => new GetSkillResponse(errorMessage, null);
    public static implicit operator GetSkillResponse(SkillRecord record) => new GetSkillResponse(null, record);
}
