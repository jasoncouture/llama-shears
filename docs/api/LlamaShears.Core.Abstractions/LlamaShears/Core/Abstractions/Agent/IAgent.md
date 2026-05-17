# LlamaShears.Core.Abstractions.Agent.IAgent

Assembly: `LlamaShears.Core.Abstractions`

An autonomous component that ingests input turns, drives a model,
and produces output turns. Identity, heartbeat cadence, channels,
and conversation state are internal and reachable through the
services that own the agent (config provider, context store,
message bus).

## Methods

### `StartAsync`(CancellationToken cancellationToken)

Starts the agent's run loop. Idempotent at construction time —
invoking it twice on the same instance throws. The owner (the
agent manager) calls this once after the agent's scope is built;
shutdown happens through scope disposal.

