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
  Loaded from two roots: `./skills` next to the installed app (bundled
  defaults, ships with the image) and `<DataRoot>/skills` (host-mounted
  overrides). On name collision the data-root variant wins — same
  pattern as templates. Per-agent allow/deny config so a tool/skill
  pack written for openclaw can be denied on this host without
  uninstalling it.
- [ ] **NuGet package plugin loading.** Download + load plugin packages from
  nuget.org-shaped feeds at runtime.
- [ ] **Plugin source flexibility.** One config field, auto-detected:
  - `Package.Name@SemVerConstraint` (latest implied if `@` part is missing)
  - `path/to/folder/Assembly.dll`
  - `some.package.nupkg`

## Agent orchestration & context
- [ ] **Cron tool — agent execution.** The tool surface, scheduler, and
  JSON store landed in PR #36; firing currently logs a stub instead of
  driving the agent. Gated on channel see/unsee (#7) — needs a
  channel-visibility model before "executes against a channel the agent
  has seen" is meaningful.
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
- [ ] **Ephemeral session support.** Sibling concept to a loaded agent
  but lifecycle is caller-owned, not config-file-driven: take the base
  `AgentConfig`, apply an in-memory overlay (alternate system prompt,
  tool allowlist, model knobs, etc.), and run inferences against it.
  Never written to disk. `AgentManager`'s reconcile loop must not see
  it as a candidate to unload — the owning subsystem manages start/stop.
  Prerequisite for the cron-tool agent execution and the agent
  heartbeat (both fire inferences against tweaked configs without
  contaminating the main agent's persistent context). Port the
  compactor's current bespoke session-setup onto this primitive once it
  lands. See [ephemeral sessions design](docs/design/ephemeral-sessions.md).
- [ ] **Smarter compaction.** Safely preserve tools (tool-call ↔ tool-result
  pairs, schema-anchored entries) and other invariants the current compactor
  can break. Plus: keep the last user turn *and* every assistant/tool turn
  that followed it, unless that suffix exceeds 25% of the model's max
  allowed tokens — in which case fall back to the trailing-user-only
  behavior. See [compaction design](docs/design/compaction.md#planned-preserve-the-trailing-user-turn-cluster).
- [ ] **On-demand tool loading.** Stop sending the full tool catalog every
  turn — currently ~10 k tokens of system prompt before the conversation
  even starts. Replace with three meta-tools (`tool_search`, `tool_load`,
  `tool_unload`); the model searches a per-agent in-memory RAG index over
  tool descriptions, loads what it needs, and the active pool is capped at
  5 slots with LRU-by-last-used eviction. See
  [tool loading design](docs/design/tool-loading.md).

## Web UI
- [ ] **Expose config.** Surface host config in the UI (read/write where
  safe).
- [ ] **Data explorer.** Per-agent UI page that walks the live data
  context scope and shows every key → value pair. Values render as
  pretty-printed JSON; on serializer failure (cycles, unmappable types,
  uncooperative converters) fall back to `value?.ToString() ?? "null"`
  so the page never blanks on one bad entry. Live counterpart to the
  generated [data-keys reference](../docs/design/data-keys.md).
- [ ] **MCP editor.** UI for adding/editing/removing MCP server
  registrations (the entries that drive `IModelContextProtocolServerRegistry`).
  Same edit affordances as the agent-config editor: JSON view with
  validation, optimistic-concurrency save.
- [ ] **Tool explorer.** Per-agent view of the full tool catalog
  discovered for the agent, plus the live active pool when the
  on-demand-loading work lands. Read-only: name, source, description,
  schema, `LastUsedAt`.
- [ ] **Skill explorer.** Browse the agent's loaded skills/playbooks
  (paired with the skills support task under Plugins & extensibility).
  Includes the per-agent block list editor — the host needs to deny a
  skill pack (e.g. an openclaw-flavored one) without uninstalling it.
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

## Documentation enforcement
- [ ] **Typed `DataKey<T>` + auto-generated data-key reference.** Replace
  the loose `const string DataKey = "..."` keys with a `DataKey<T>`
  type (non-generic `DataKey` base for the dictionary key, `T` for the
  value side, implicit `→ string` for transitional dict access).
  Default key from `nameof(T)`; explicit string when callers want a
  different scriban name. Analyzer enforces an XML `<summary>` on every
  `DataKey<T>` field (one-liner). A docs target walks the assembly,
  emits a `## Template data` table where each row links to the *generic
  parameter*'s class docs (not `DataKey<T>` itself), with the field's
  one-liner as the row text. See [data-keys design](docs/design/data-keys.md).

## Build / infrastructure
- [ ] **Cron `FireSingleAsync` lost-update window.** `JsonCronStore`
  read-modify-writes the whole file on every upsert, so two ticks (or a
  manual `Trigger` racing the system tick) firing the same job in parallel
  can clobber `LastFiredAt` / `NextFireAt`. Today's `SemaphoreSlim` only
  serialises within a process; nothing protects against a stale snapshot
  re-overwriting fresh state. Real fix is a transactional update — re-read
  the file inside the gate (or check a per-job version) before the atomic
  rename — and is out of scope for the cron stub PR (#36) due to
  complexity. Track here until it lands.
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
