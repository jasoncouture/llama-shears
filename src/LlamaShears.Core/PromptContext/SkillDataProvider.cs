using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.PromptContext;

namespace LlamaShears.Core.PromptContext;

public class SkillDataProvider : IDataContextItemProvider
{
    private readonly IDataContextScope _scope;
    private readonly ISkillRepository _skillRepository;

    public SkillDataProvider(IDataContextScope scope, ISkillRepository skillRepository)
    {
        _scope = scope;
        _skillRepository = skillRepository;
    }

    public async Task<IEnumerable<KeyValuePair<string, object?>>> GetItemsForCurrentContext(
        CancellationToken cancellationToken = default)
    {
        var agentId = _scope.GetAgentConfig().Id;
        var skillInfo = await _skillRepository.GetSkillsAsync(agentId, cancellationToken);
        return [new KeyValuePair<string, object?>(AgentSkills.DataKey, skillInfo)];
    }
}