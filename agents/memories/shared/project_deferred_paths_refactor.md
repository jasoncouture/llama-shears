---
name: Deferred — IShearsPaths refactor + config.json pre-DI lookup
description: Open work item from 2026-04-28 evening. Refactor LlamaShearsPaths from process-global static to instance-via-interface; resolve the pre-DI config.json lookup question. Picked up 2026-04-29.
type: project
---

Pulled out of the bearer-auth thread on 2026-04-28; deferred to 2026-04-29.

**Task 1 (committed direction):** Convert `LlamaShearsPaths` from a process-global static class to an injectable `IShearsPaths` interface. The static caching is what forces integration tests onto a single shared host fixture per process — once the refactor lands, tests can override paths per-fixture via DI and the constraint dissolves.

Sketch:
- `IShearsPaths` in `Hosting.Abstractions`: `DataRoot`, `WorkspaceRoot`, `AgentsRoot`, `TemplatesRoot`, `ConfigFile`, `GetAgentWorkspaceDefaultPath(name)`.
- `ShearsPathsOptions` (`DataRoot?`, `WorkspaceRoot?`, `AgentsRoot?`, `TemplatesRoot?`) bound from a `Paths:` config section.
- Default impl `ShearsPaths` resolves env-or-options-or-default at construction.
- Static class shrinks to constants only (env var names, default subfolder names).
- Sweep callers: `AgentManager.Reconcile` (reads `LlamaShearsPaths.AgentsRoot`), and the pre-DI `AddLlamaShearsUserConfiguration` — which is task 2.
- Backward-compat env var bridging: `LLAMA_SHEARS_DATA_ROOT` etc. continue to work, mapped to `Paths:DataRoot` etc. via a small custom config source (~10 lines) so existing operators / scripts / tests don't break.

**Task 2 (open question):** `AddLlamaShearsUserConfiguration` runs at config-build time, before DI exists, and needs to know where `config.json` lives. Once the static is gone we need an answer.

Three options on the table from the conversation:
- (A) Drop `config.json` for now (greenfield, nothing seems to depend on it; verify with grep). Operators configure via `appsettings.json` + env vars. Reintroduce when there's a concrete user need.
- (B) Inline a three-line env-or-default lookup at the pre-DI extension site, factored as a pure static helper on the constants class (no caching, no instance) so the DataRoot resolution stays in sync with `IShearsPaths`.
- (C) Drive the user-config path from an `appsettings.json` setting like `Paths:UserConfigPath`. More moving parts than B, no clear win.

Conversation lean was (B) — keep `config.json` for operator UX, accept one duplicated three-liner pre-DI. User flagged uncertainty and asked to defer the decision; revisit before doing task 1's sweep (the answer affects the static class's final shape).
