using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Agent.Core.SystemPrompt;

public interface ISystemPromptDataProvider
{
    SystemPromptModel Build(IAgent agent);
}
