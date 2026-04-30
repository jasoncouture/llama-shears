using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Composed, immutable snapshot of an agent's runtime state. Materialized
/// on demand by an external composer service from the authoritative
/// sources of each child slice. Templates, observers, and plugins read
/// the tree directly; mutations happen through scope-specific writer
/// services, never through this record.
/// </summary>
public sealed record AgentContext(
    AgentConfig Config,
    AgentInfo Info,
    LanguageModelContext LanguageModel,
    SystemContext System,
    PluginContext Plugins,
    TurnContext? Turn = null);
