using LlamaShears.Agent.Abstractions;

namespace LlamaShears.Agent.Core.SystemPrompt;

public interface ISystemPromptDataProvider
{
    SystemPromptModel Build(IAgent agent);
}
