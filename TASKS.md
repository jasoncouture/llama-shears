# Tasks

Capture file for ideas and followups that surface during a session and need a
home before they drift. Group by area; trim freely.

> Initial population: 2026-05-07 brain-dump.

## Providers
- [ ] **OpenAI / OpenAI-compatible.** Mirror `LlamaShears.Provider.Ollama`'s
  shape. `llama-server` exposes `/v1/chat/completions`, `/v1/completions`,
  `/v1/embeddings`, and `/v1/models` natively, so the same provider covers any
  OpenAI-API-compatible local server (vLLM, LM Studio, TabbyAPI, etc.).
  Open: per-agent base URL? Tool-call schema (OpenAI's function-call shape vs
  Ollama's flatter form)?
- [ ] **llama.cpp native API.** Separate provider targeting `llama-server`'s
  *native* endpoints (`/completion`, `/embedding`, slot management) for the
  extra knobs OpenAI-compat hides — logprobs, multi-slot batching,
  finer-grained sampling controls.

## Plugins & extensibility
- [ ] **Skills support.** Plugin-side equivalent to skill files / playbooks.
- [ ] **NuGet package plugin loading.** Download + load plugin packages from
  nuget.org-shaped feeds at runtime.
- [ ] **Plugin source flexibility.** One config field, auto-detected:
  - `Package.Name@SemVerConstraint` (latest implied if `@` part is missing)
  - `path/to/folder/Assembly.dll`
  - `some.package.nupkg`

## Agent orchestration & context
- [ ] **Cron tool.** Lets an agent schedule its own future actions; when the
  scheduled time fires, the execution runs as the agent against a channel the
  agent has visibility into.
- [ ] **Channel see / unsee.** Grant or revoke an agent's visibility into a
  channel — paired affordances; an agent that can see channels at runtime
  also needs a way to stop seeing one.
- [ ] **Sub-agents.** An agent can spawn another agent to handle a delimited
  task, receive the result, and continue.
- [ ] **Sub-agent depth limits.** Configurable max spawn depth plus a
  per-tree budget, so a runaway parent can't infinitely recurse.
- [ ] **Transient controllable contexts.** Agent can carve scratch contexts
  to hold a task's working state without bloating its main context window or
  forcing compaction.
- [ ] **Smarter compaction.** Safely preserve tools (tool-call ↔ tool-result
  pairs, schema-anchored entries) and other invariants the current compactor
  can break.

## Web UI
- [ ] **Agent creator/editor.** Build / edit agent JSON from the UI.
- [ ] **Expose config.** Surface host config in the UI (read/write where
  safe).
- [ ] **Self-restart control.** Restart-the-app button.
- [ ] **Interrupt in-flight agent.** Cancel an in-flight turn from the UI.
- [ ] **View archived sessions.** Browse compaction-archived context
  (`<Context>/<agent>/<unix-ms>.json`) from the UI as read-only history
  alongside the live conversation.
- [ ] **Attachment-types discovery.** UI asks the host what kinds of
  attachments it can accept (images today, more later — see "additional
  attachment types" below).
- [ ] **Optional OAuth authentication.** Login wall so the UI can be safely
  exposed to the public internet.

## Other surfaces / clients
- [ ] **Desktop app.**
- [ ] **Mobile apps** — up in the air; depends on whether the desktop app's
  bones reuse cleanly.
- [ ] **Console app, stdio/stdin MCP integration.** Headless agent that
  speaks MCP over stdio for embedding into other tooling.
- [ ] **Text UI.** TUI for the chat surface.
- [ ] **Channel adapters.** Discord, Telegram, Signal — each as a
  `ChannelMessage` source/sink.

## Tools & security
- [ ] **Tool security model.** Authorization for which agents / tools /
  scopes can run what.
- [ ] **Unsafe tools.** Shell execution, background processes — gated behind
  explicit permission (per-agent, per-call, or both).

## Build / infrastructure
- [ ] **Missing `README.md` in `src/public/` is a build failure.** Wire the
  docs-build target (or a sibling MSBuild target) to fail when a project
  under `src/public/` has no `README.md`. Pit-of-success enforcement of
  the "every public package needs a README" rule. No GH issue — this
  stays as a TASKS.md item until the enforcement lands.
- [ ] **Replace `irongut/CodeCoverageSummary`.** The action backing the
  per-assembly markdown coverage summary in the run-tests composite
  action looks abandoned — last commit in 2022, no major-floating tag.
  Currently pinned to `@v1.3.0`. Find a maintained replacement (or roll
  our own simple cobertura→markdown step) before the existing pin
  bit-rots.

## Misc
- [ ] **Additional attachment types.** Beyond images — text files, PDFs,
  audio, whatever the active model can ingest.
- [ ] **Agent heartbeat.** Periodic system-input independent of user messages
  (per the heartbeat-isn't-the-chat-trigger note).
- [ ] **Refinement / simplification pass.** General "tighten what we have
  before adding more" sweep.
