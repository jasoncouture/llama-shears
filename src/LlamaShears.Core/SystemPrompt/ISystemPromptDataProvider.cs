using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.SystemPrompt;

public interface ISystemPromptDataProvider
{
    SystemPromptModel Build(IAgent agent);
}
