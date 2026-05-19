# LlamaShears.Core.AgentHandle

Assembly: `LlamaShears.Core.Abstractions`

Owns the resources of one running agent instance: the DI scope, the captured
ExecutionContext the agent boots into, the underlying
[IAgent](Abstractions/Agent/IAgent.md)'s background run-task, and any child [AgentHandle](AgentHandle.md)s
spawned from it. Created cold by [IAgentFactory](IAgentFactory.md); goes hot when [AgentHandle](AgentHandle.md).`Start`
is invoked. Disposing tears down children first, then the scope, then awaits the run-task.

## Parameters

- `SessionPath` — Identity of this agent's session, including parent/root chain.
- `ConfigHash` — Hash of the [AgentConfig](Abstractions/Agent/AgentConfig.md) the handle was built against.
- `Scope` — DI scope owned by the handle; disposed on teardown.
- `ExecutionContext` — Blank execution context the agent loop runs under.

## Properties

### `AgentTask`

Background task returned by [IAgent](Abstractions/Agent/IAgent.md).`RunAsync`; `null` until [AgentHandle](AgentHandle.md).`Start` is called.

### `ConfigHash`

Hash of the [AgentConfig](Abstractions/Agent/AgentConfig.md) the handle was built against.

### `ExecutionContext`

Blank execution context the agent loop runs under.

### `Running`

`true` if started and the agent task has not yet completed.

### `Scope`

DI scope owned by the handle; disposed on teardown.

### `SessionPath`

Identity of this agent's session, including parent/root chain.

### `Started`

`true` once [AgentHandle](AgentHandle.md).`Start` has been called.

## Methods

### `AgentHandle`([SessionPath](Abstractions/Agent/Sessions/SessionPath.md) SessionPath, string ConfigHash, AsyncServiceScope Scope, ExecutionContext ExecutionContext)

Owns the resources of one running agent instance: the DI scope, the captured
ExecutionContext the agent boots into, the underlying
[IAgent](Abstractions/Agent/IAgent.md)'s background run-task, and any child [AgentHandle](AgentHandle.md)s
spawned from it. Created cold by [IAgentFactory](IAgentFactory.md); goes hot when [AgentHandle](AgentHandle.md).`Start`
is invoked. Disposing tears down children first, then the scope, then awaits the run-task.

#### Parameters

- `SessionPath` — Identity of this agent's session, including parent/root chain.
- `ConfigHash` — Hash of the [AgentConfig](Abstractions/Agent/AgentConfig.md) the handle was built against.
- `Scope` — DI scope owned by the handle; disposed on teardown.
- `ExecutionContext` — Blank execution context the agent loop runs under.

### `DestroyChild`(Guid id)

Removes and disposes a descendant anywhere in this handle's subtree. Walks children
recursively until the target is found.

#### Returns

`true` if the descendant was found and disposed.

### `DisposeAsync`

Disposes children first, then the scope, then awaits the run-task.

### `Start`

Starts the agent loop on the captured execution context. Idempotent guard throws if called twice.

### `TryAddChild`([AgentHandle](AgentHandle.md) child)

Attempts to attach `child` under this handle; returns `false` if a child with the same id is already present.

### `TryRemoveChild`(Guid id, [AgentHandle](AgentHandle.md)& child)

Removes a direct child by id without disposing it.

