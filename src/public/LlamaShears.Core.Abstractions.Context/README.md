# LlamaShears.Core.Abstractions.Context

Per-turn context contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). An agent's *context* is the composed snapshot of "everything a tool, compactor, or plugin needs to know about the current agent right now" — the conversation slice the model sees, the agent's tool catalog, free-form plugin state, and host metadata.

## Public surface

- **`AgentContext`** — the aggregate snapshot (id, now, config, language model context, system context, tool context, plugin context).
- **`IAgentContextProvider`** — host-side builder for `AgentContext` snapshots.
- **`IContextCompactor`** — replaces a context window with a summarized version when token pressure demands it.
- **`LanguageModelContext`** — turns + raw entries + the model's context-window size.
- **`SystemContext`** — host metadata visible to the agent.
- **`ToolContext`** — flat tool catalog visible to the agent for this snapshot.
- **`PluginContext`** — free-form data bag plugins use to thread state through context.

## See also

- [Agent loop](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/agent-loop.md)
- [Context compaction](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/compaction.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
