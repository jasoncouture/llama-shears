# LlamaShears.Core.Abstractions.Context

## Types

- [AgentContext](AgentContext.md) — Composed snapshot of everything a tool, compactor, or plugin needs to know about "the current agent right now". Built on demand by [IAgentContextProvider](IAgentContextProvider.md); components consume slices ([AgentContext](AgentContext.md).`LanguageModel`, [AgentContext](AgentContext.md).`Tools`, etc.) rather than reaching back into the host's services directly.
- [IAgentContextProvider](IAgentContextProvider.md) — Composes [AgentContext](AgentContext.md) snapshots on demand from the host's authoritative sources (config, language model, plugins, etc.). Returns `null` when no context can be built — for the parameterless overload, when there is no ambient agent; for the id-bearing overload, when the agent does not exist.
- [IContextCompactor](IContextCompactor.md) — Decides whether a [ModelPrompt](../Provider/ModelPrompt.md) exceeds the model's context window and, if so, rewrites it so the next model call fits — typically by summarizing earlier turns into a single assistant message and preserving the trailing user turn. Pure w.r.t. external storage; callers archive any displaced context themselves.
- [LanguageModelContext](LanguageModelContext.md) — Conversation slice of an [AgentContext](AgentContext.md) snapshot: chronological turns, the polymorphic entry log they were drawn from, and the model's current context-window size in tokens.
- [PluginContext](PluginContext.md) — Free-form key/value bag plugins use to surface state on an [AgentContext](AgentContext.md) snapshot. Keys are namespaced by the owning plugin to avoid collisions; the framework treats values as opaque.
- [SystemContext](SystemContext.md) — Reserved system slice on an [AgentContext](AgentContext.md) snapshot. Empty today; held in place so future host-level metadata can be added without churning the surrounding contract.
- [ToolContext](ToolContext.md) — The flat tool catalog visible to the agent on an [AgentContext](AgentContext.md) snapshot. The grouped form (ToolGroup) is for prompts; this flat form is what templates and tools iterate over.

