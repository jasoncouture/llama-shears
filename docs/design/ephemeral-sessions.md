# Ephemeral sessions

Several features want to fire an inference *as the agent* without contaminating the main agent loop:

- **Context compaction.** Today the compactor builds its own prompt, scopes its own current-agent info, and walks a private inference loop. The shape of what it does is "run as the agent, but with a different system prompt and a stripped tool list, against a synthetic conversation."
- **Cron triggers.** When a scheduled job fires, the agent needs to run a turn driven by the cron prompt — possibly with a different system prompt, possibly with a one-shot mode that doesn't persist anything.
- **Heartbeat.** Periodic system-injected input that wants to look like a turn but is run against an alternate prompt path.

All three end up wanting the same thing: a *configured agent* whose lifecycle is owned by the caller, not the on-disk config file, and whose state never persists.

## Shape

An **ephemeral session** is:

- A base `AgentConfig` (usually the live agent's), plus an in-memory **overlay** that adjusts a subset of fields: `SystemPrompt`, `PromptContext`, `Model` overrides (token limit, tool list, sampling tweaks), tool allow/deny, and so on. The overlay is a record-with semantics so it composes cleanly.
- A data-context scope, populated the same way the main agent's is (workspace, agent config, model config, todo list, etc.) but with the overlay applied where it changes anything.
- An inference path that consumes the overlaid prompt/tools and writes results back to wherever the caller asked — usually nowhere, sometimes a one-off log, never the durable `<Context>/<agent>/...` files.

The session is bound to its owning subsystem. `AgentManager`'s reconcile loop knows about it but does **not** consider it a candidate for unload — it didn't start the session, so it doesn't stop the session.

## Lifecycle

```
session = await sessionFactory.CreateAsync(baseAgentId, overlay, cancellationToken);
try
{
    var result = await session.RunAsync(prompt, cancellationToken);
    // act on result
}
finally
{
    await session.DisposeAsync();
}
```

The factory:

1. Resolves the base agent (must be loaded; ephemeral sessions don't reanimate stopped agents).
2. Clones the base config with the overlay applied.
3. Opens a fresh data-context frame keyed on `<baseAgentId>:<sessionId>` or similar — distinct from the main agent's scope so writes don't leak back.
4. Seeds the scope with whatever the overlay's policy demands (e.g. compactor wants `AgentConfig` and `WorkspaceContext` but explicitly *not* the live `TodoList`).
5. Hands back an `IEphemeralAgentSession` the caller can `RunAsync` against until it disposes.

`AgentManager` learns about the session through `ISessionRegistry` (or similar) so its reconcile path can skip the ephemeral entries instead of trying to start/stop them. Disposal removes the entry; if the caller forgets to dispose, the data-context scope's weak reference eventually GCs the frame, but the session registry leaks a logical slot — call disposal a hard requirement.

## What the overlay covers

First pass:

- `SystemPrompt` (template file name)
- `PromptContext` (template file name)
- `Tools.TurnLimit`
- `ModelContextProtocolServers` (allowlist, narrowing only — never broadening)
- `Model` parameter overrides — `TokenLimit`, the open-ended provider parameters dict.

Not covered initially (and probably never):

- `WorkspacePath` — sessions share the agent's workspace.
- `Id` — the session is identified by the base agent + a sub-id, not a new agent name.
- `Embedding` — keeps the same memory store.

Compactor's needs already fit the first list. Cron and heartbeat will exercise the same surface with different overlay values.

## Why this is a prereq for cron and heartbeat

Both want to drive the agent through a fresh inference at a chosen moment without changing the agent's persistent state until the result is decided. Today's "interrupt the loop and push a synthetic ChannelMessage" approach works for plain prompts but breaks down the moment cron/heartbeat want a different system prompt, a one-shot mode that doesn't write turns to the context store, or a tighter tool allowlist.

An ephemeral session is the natural home: hand the firing subsystem a configured session, let it run, dispose. The main agent loop never sees the work; the on-disk context never grows from it; the compaction trigger never moves.

## Migration: lift the compactor onto it

The compactor's current implementation is the working sketch. Concretely:

1. `IEphemeralSessionFactory` interface; reference impl lives in `LlamaShears.Core`.
2. `IEphemeralAgentSession` exposing `RunAsync(prompt, options, cancellationToken)` and `IAsyncDisposable`.
3. `AgentManager` learns to skip ephemeral entries in `ReconcileAsync` (by `ISessionRegistry` lookup).
4. Refactor `ContextCompactor.SummarizeAsync` to:
   - request a session with the compaction overlay,
   - run its existing tool-loop driver against the session's `RunAsync`,
   - dispose.
5. Once compactor is on the new surface and its tests still pass, the same primitive is ready for cron and heartbeat.

## Risks

- **Scope leak.** Ephemeral writes into shared singletons (memory store, todo store, file system) still hit shared state. The overlay only constrains *what the agent sees*; it doesn't sandbox side effects. Document this loudly: ephemeral ≠ sandboxed.
- **Resource accounting.** Long-running ephemeral sessions (cron job that streams for minutes) consume the same model-provider connection pool as the main loop. If both fire at once, the model server sees concurrent requests. Acceptable for now; revisit if observed.
- **Cancellation propagation.** The owning subsystem must wire its own `CancellationToken` through to the session. Don't piggyback on the main agent's loop cancellation — that's exactly the coupling this primitive removes.
