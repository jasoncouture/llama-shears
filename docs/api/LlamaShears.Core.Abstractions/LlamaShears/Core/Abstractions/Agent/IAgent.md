# LlamaShears.Core.Abstractions.Agent.IAgent

Assembly: `LlamaShears.Core.Abstractions`

An autonomous component that ingests input turns, drives a model,
and produces output turns. Identity, heartbeat cadence, channels,
and conversation state are internal and reachable through the
services that own the agent (config provider, context store,
message bus).

## Methods

### `RunAsync`

Run the main agent loop

