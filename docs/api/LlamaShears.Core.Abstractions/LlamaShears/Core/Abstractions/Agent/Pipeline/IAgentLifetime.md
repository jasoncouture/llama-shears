# LlamaShears.Core.Abstractions.Agent.Pipeline.IAgentLifetime

Assembly: `LlamaShears.Core.Abstractions`

Per-agent stop signal, analogous to
`IHostApplicationLifetime` for one agent scope. The loop
watches [IAgentLifetime](IAgentLifetime.md).`Stopping`; inbound shutdown handling and
dispose call [IAgentLifetime](IAgentLifetime.md).`Stop`.

## Properties

### `Stopping`

Trips when [IAgentLifetime](IAgentLifetime.md).`Stop` has been called. The loop uses
this as its run-loop cancellation token.

## Methods

### `Stop`

Requests that the agent loop exit. Idempotent. Does not wait
for the loop to finish — callers that need to join do that
themselves.

