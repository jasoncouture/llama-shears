using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Core.Abstractions.Context;

/// <summary>
/// Composed, immutable snapshot of an agent's runtime state. Materialized
/// on demand by an external composer service from the authoritative
/// sources of each child slice. Templates, observers, and plugins read
/// the tree directly; mutations happen through scope-specific writer
/// services, never through this record.
/// <para>
/// The top level intentionally exposes two primitives — <see cref="AgentId"/>
/// and <see cref="Now"/> — because they're load-bearing for almost every
/// consumer. Everything else lives under a typed child slice. Scope
/// (turn/agent/global) is a writer-side concern: by the time a snapshot
/// is composed, the appropriate plugin items for the moment have already
/// been folded into <see cref="PluginContext.Items"/>.
/// </para>
/// </summary>
public sealed record AgentContext(
    string AgentId,
    DateTimeOffset Now,
    AgentConfig Config,
    LanguageModelContext LanguageModel,
    SystemContext System,
    PluginContext Plugins);
