using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Composed snapshot of everything a tool, compactor, or plugin needs
/// to know about "the current agent right now". Built on demand by
/// <see cref="IAgentContextProvider"/>; components consume slices
/// (<see cref="LanguageModel"/>, <see cref="Tools"/>, etc.) rather
/// than reaching back into the host's services directly.
/// </summary>
/// <param name="AgentId">Identifier of the agent the snapshot is built for.</param>
/// <param name="Now">Wall-clock time captured when the snapshot was created.</param>
/// <param name="Config">The agent's loaded configuration snapshot.</param>
/// <param name="LanguageModel">Conversation log slice (turns, raw entries, model context-window size).</param>
/// <param name="System">System-level context (host metadata, etc.).</param>
/// <param name="Tools">Tool catalog visible to the agent for this snapshot.</param>
/// <param name="Plugins">Free-form data bag plugins use to thread state through context.</param>
public sealed record AgentContext(
    string AgentId,
    DateTimeOffset Now,
    AgentConfig Config,
    LanguageModelContext LanguageModel,
    SystemContext System,
    ToolContext Tools,
    PluginContext Plugins);
