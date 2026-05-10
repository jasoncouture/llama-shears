# LlamaShears.Core.Abstractions.Agent

Agent-identity, configuration, and persistence contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). Compiled against by anything that needs to know what an agent **is** — the host, the chat UI, plugins that interact with agents, and tests.

## Public surface

- **`AgentConfig`** — the on-disk per-agent configuration record (model, embedding, memory, tools, channels, heartbeat).
- **`AgentInfo`** — runtime identity (id, display name) for an active agent.
- **`AgentModelConfig`**, **`AgentEmbeddingConfig`**, **`AgentMemoryConfig`**, **`AgentToolConfig`** — per-section config shapes.
- **`IAgent`** — the live agent surface (channel input, context handle).
- **`IAgentManager`** — ownership of running agents; reconcile and lookup.
- **`IAgentConfigProvider`** — the watched `<Data>/agents/*.json` source.
- **`IAgentTokenStore`** — single-use bearer tokens for agent-bound MCP requests.
- **`SystemTick`** — the periodic signal the host publishes; agents consume their heartbeat off it.
- **`Persistence/IAgentContext`**, **`Persistence/IContextStore`**, **`Persistence/ArchiveId`** — contracts for the JSON-lines context store and its compaction-archive identifiers.

## See also

- [Architecture overview](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/architecture.md)
- [Agent configuration](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/agent-config.md)
- [Agent loop](https://github.com/jasoncouture/llama-shears/blob/main/docs/design/agent-loop.md)
- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.
