using LlamaShears.Agent.Abstractions;

namespace LlamaShears.Agent.Core.SystemPrompt;

public sealed class SystemPromptDataProvider : ISystemPromptDataProvider
{
    private readonly TimeProvider _time;

    public SystemPromptDataProvider(TimeProvider time)
    {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
    }

    public SystemPromptModel Build(IAgent agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        return new SystemPromptModel
        {
            Workspace = new WorkspaceContext
            {
                Directory = string.Empty,
            },
            Tools = new ToolsContext
            {
                List = string.Empty,
            },
            Runtime = new RuntimeContext
            {
                Line = string.Empty,
                ReasoningLevel = ReasoningLevel.Medium,
                Timezone = _time.LocalTimeZone,
            },
        };
    }
}
