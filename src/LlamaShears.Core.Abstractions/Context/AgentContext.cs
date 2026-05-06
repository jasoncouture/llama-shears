using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Context;

public sealed record AgentContext(
    string AgentId,
    DateTimeOffset Now,
    AgentConfig Config,
    LanguageModelContext LanguageModel,
    SystemContext System,
    ToolContext Tools,
    PluginContext Plugins);
