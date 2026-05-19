# LlamaShears.Core

## Types

- [AgentHandle](AgentHandle.md) — Owns the resources of one running agent instance: the DI scope, the captured ExecutionContext the agent boots into, the underlying [IAgent](Abstractions/Agent/IAgent.md)'s background run-task, and any child [AgentHandle](AgentHandle.md)s spawned from it. Created cold by [IAgentFactory](IAgentFactory.md); goes hot when [AgentHandle](AgentHandle.md).`Start` is invoked. Disposing tears down children first, then the scope, then awaits the run-task.
- [AgentSessionPath](AgentSessionPath.md) — Materialised parent-chain of session ids for an agent, ordered root-last. Built lazily by the repository when full ancestry is needed for logging or routing.
- [CombinedDisposable](CombinedDisposable.md) — Extension methods that chain disposables into a single [DisposableList](DisposableList.md). Folds the resulting list when either side is already a list, so a chain of `.And(x).And(y).And(z)` stays flat.
- [DisposableList](DisposableList.md) — Composite disposable that owns a LIFO stack of mixed IDisposable and IAsyncDisposable instances. Disposal walks the stack in reverse order, catches per-item exceptions, and rethrows as an AggregateException. Sync IDisposable.`Dispose` blocks on the async path — async-first by design.
- [IAgentFactory](IAgentFactory.md) — Spawns a clean agent state: blank execution context, fresh DI scope, fresh keyed data context seeded with the supplied [AgentConfig](Abstractions/Agent/AgentConfig.md) plus any caller-supplied overlay data, eager-resolved language model, and a started [IAgent](Abstractions/Agent/IAgent.md). Returns the [AgentHandle](AgentHandle.md) that owns the resulting scope.
- [IAgentInstanceRepository](IAgentInstanceRepository.md) — Tracks every [AgentHandle](AgentHandle.md) across the host, keyed by session id, with knowledge of the parent/child graph. Enumerations expose handles in safe disposal order.

