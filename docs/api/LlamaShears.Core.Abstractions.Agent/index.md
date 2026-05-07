# LlamaShears.Core.Abstractions.Agent

Agent-identity, configuration, and persistence contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). Compiled against by anything that needs to know what an agent **is** — the host, the chat UI, plugins that interact with agents, and tests.

## Public surface

- **`AgentConfig`** / **`AgentConfiguration`** — the on-disk per-agent configuration record (model, embedding, memory, tools, channels, heartbeat).
- **`AgentInfo`** — runtime identity (id, display name) for an active agent.
- **`AgentModelConfig`**, **`AgentEmbeddingConfig`**, **`AgentMemoryConfig`**, **`AgentToolConfig`** — per-section config shapes.
- **`IAgent`** — the live agent surface (channel input, context handle).
- **`IAgentManager`** — ownership of running agents; reconcile and lookup.
- **`IAgentFactory`** — host-side construction.
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

---

## LlamaShears.Core.Abstractions.Agent

- [AgentConfig](LlamaShears/Core/Abstractions/Agent/AgentConfig.md)
- [AgentConfiguration](LlamaShears/Core/Abstractions/Agent/AgentConfiguration.md)
- [AgentEmbeddingConfig](LlamaShears/Core/Abstractions/Agent/AgentEmbeddingConfig.md)
- [AgentInfo](LlamaShears/Core/Abstractions/Agent/AgentInfo.md)
- [AgentMemoryConfig](LlamaShears/Core/Abstractions/Agent/AgentMemoryConfig.md)
- [AgentModelConfig](LlamaShears/Core/Abstractions/Agent/AgentModelConfig.md)
- [AgentToolConfig](LlamaShears/Core/Abstractions/Agent/AgentToolConfig.md)
- [IAgent](LlamaShears/Core/Abstractions/Agent/IAgent.md)
- [IAgentConfigProvider](LlamaShears/Core/Abstractions/Agent/IAgentConfigProvider.md)
- [IAgentFactory](LlamaShears/Core/Abstractions/Agent/IAgentFactory.md)
- [IAgentManager](LlamaShears/Core/Abstractions/Agent/IAgentManager.md)
- [IAgentTokenStore](LlamaShears/Core/Abstractions/Agent/IAgentTokenStore.md)
- [SystemTick](LlamaShears/Core/Abstractions/Agent/SystemTick.md)

## LlamaShears.Core.Abstractions.Agent.Persistence

- [ArchiveId](LlamaShears/Core/Abstractions/Agent/Persistence/ArchiveId.md)
- [IAgentContext](LlamaShears/Core/Abstractions/Agent/Persistence/IAgentContext.md)
- [IContextStore](LlamaShears/Core/Abstractions/Agent/Persistence/IContextStore.md)

