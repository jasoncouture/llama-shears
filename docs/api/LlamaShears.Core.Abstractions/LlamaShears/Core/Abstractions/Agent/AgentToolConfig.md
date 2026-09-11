# LlamaShears.Core.Abstractions.Agent.AgentToolConfig

Assembly: `LlamaShears.Core.Abstractions`

Per-agent tool-loop guardrails.

## Properties

### `TurnLimit`

Maximum model rounds one inbound user or framework-user
batch may drive. Zero (the default) is unlimited. A
positive `N` runs iterations 1..`N-1` with the
full tool catalog and iteration `N` with none; leftover
tool calls on that last round are dropped so the queue
can drain. This default stays unlimited so existing agents
are not silently capped.

