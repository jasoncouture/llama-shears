# System prompts and prompt context

Two distinct pieces of text get fed to the model on every turn:

1. **The system prompt** — the persistent "you are X, here's how you behave" preamble. Stable across iterations within a batch; rebuilt fresh each batch from the workspace template.
2. **The ephemeral prompt-context block** — a `SystemEphemeral`-roled turn injected immediately before the most recent `User` turn. Carries time, identity files, memory hits, and the optional important-message banner. Rebuilt every iteration; never persisted.

Both are Scriban templates. The renderer is [`TemplateRenderer`](../../src/LlamaShears.Core/Templating/TemplateRenderer.cs); the providers are [`FilesystemSystemPromptProvider`](../../src/LlamaShears.Core/SystemPrompt/FilesystemSystemPromptProvider.cs) and [`FilesystemPromptContextProvider`](../../src/LlamaShears.Core/PromptContext/FilesystemPromptContextProvider.cs).

## System prompt

### What it is

`Agent` calls `_systemPrompt.GetAsync(_config.SystemPrompt, BuildSystemPromptParameters(), ct)` once per batch and uses the result as the `Content` of the `System`-role turn at position 0 of `prompt.Turns`. The system prompt is *not* persisted — it's reconstructed every batch.

`AgentConfig.SystemPrompt` selects the template by name (no extension, no path separators). Default is `"DEFAULT"`.

### Template parameters

```csharp
public sealed record SystemPromptTemplateParameters(
    string AgentId,
    string? WorkspacePath,
    int ToolCallTurns);     // == AgentConfig.Tools.TurnLimit
```

These surface in Scriban as `agent_id`, `workspace_path`, `tool_call_turns`.

### Fallback chain

```
<workspace>/system/<name>.md
<workspace>/system/DEFAULT.md
<bundled>/<name>.md
<bundled>/DEFAULT.md
```

The first that exists wins. `<bundled>` is `AppContext.BaseDirectory/content/templates/workspace/system/` (overridable via `FilesystemSystemPromptOptions.BundledRoot`). If none of the four candidates exists, the provider throws `FileNotFoundException` — that's a deployment problem, not a runtime one.

The double-fallback covers two intents:

- *"I want to override this agent's system prompt"* → put a `DEFAULT.md` (or named variant) in the agent's `system/` directory.
- *"I want to override the project-wide default"* → edit `<Templates>/workspace/system/DEFAULT.md` (the operator-editable copy of the bundled tree).

### Bundled `DEFAULT.md`

The bundled prompt at [`src/LlamaShears/content/templates/workspace/system/DEFAULT.md`](../../src/LlamaShears/content/templates/workspace/system/DEFAULT.md) covers safety guardrails, tool-call style guidance, parallel-tool encouragement, output directives, and the `<system>...</system>` message-prefix convention. It's a sensible default to inherit; replace it for an agent that needs a different posture.

`MINIMAL.md` is a stripped-down variant for headless runs that don't need the full persona scaffolding (cron tasks, single-shot batch work). Selection is per-agent via `AgentConfig.SystemPrompt: "MINIMAL"`.

`SUBAGENT.md` is the seed for sub-agent prompts; sub-agent spawning isn't implemented yet.

## The ephemeral prompt-context block

### What it is

`Agent.InjectPromptContextAsync` calls `_promptContext.GetAsync(_config.PromptContext, parameters, ct)` once per *iteration*, finds the most recent `User`-roled turn in the prompt, and inserts a `SystemEphemeral`-roled turn immediately before it.

This block is the framework's single coherent place to inject *everything that's true right now* without having to chain it through the persistent context:

- Wall-clock time (so the model knows what "now" is on this turn).
- The current channel id.
- An `important_message` (used today for the final-iteration "tools are gone, write text" notice).
- The conventional workspace files: `BOOTSTRAP.md`, `IDENTITY.md`, `SOUL.md` — in that order.
- Memory hits (path, content, score) returned by the per-batch RAG search.
- A name-only listing of every other root-level `.md` in the workspace, so the model knows what's there without paying token cost for the bodies.

### Template parameters

```csharp
public sealed record PromptContextParameters(
    string Now,                       // ISO-8601 local time
    string Timezone,                  // TimeZoneInfo.Local.Id
    string DayOfWeek,                 // e.g. "Tuesday"
    string? ChannelId,
    string? ImportantMessage,
    string? WorkspacePath)
{
    public IReadOnlyList<PromptContextFile> Files { get; init; } = [];
    public IReadOnlyList<string> AdditionalFiles { get; init; } = [];
    public IReadOnlyList<PromptContextMemory> Memories { get; init; } = [];
}
```

`Files` is populated by the provider (the conventional file list); `AdditionalFiles` is the name-only listing of other `.md`s in the workspace root; `Memories` is set by the agent's per-batch search.

### Fallback chain

Same shape as the system prompt, but rooted at `system/context/`:

```
<workspace>/system/context/<name>.md
<workspace>/system/context/PROMPT.md
<bundled>/<name>.md
<bundled>/PROMPT.md
```

Default name is `PROMPT`. Unlike the system prompt provider, the prompt-context provider returns `null` when *every* candidate fails to render to non-empty text, and the agent loop responds by skipping the injection — the system prompt describes the prefix as optional.

### Bundled `PROMPT.md`

[`src/LlamaShears/content/templates/workspace/system/context/PROMPT.md`](../../src/LlamaShears/content/templates/workspace/system/context/PROMPT.md):

```scriban
<system>
{{- if important_message }}
- IMPORTANT: {{ important_message }}
{{- end }}
- Current date and time: {{ now }}
- Current timezone:{{ timezone }}
- Current day of week: {{ day_of_week }}
{{- if channel_id }}
- Channel: {{ channel_id }}
{{- end }}
{{- if memories.size > 0 }}

## Memory search matches
...
{{- end }}
{{- for file in files }}

## {{ file.name }}

{{ file.content }}
{{- end }}
{{- if additional_files.size > 0 }}

## Additional workspace files

{{- for name in additional_files }}
- {{ name }}
{{- end }}
{{- end }}
</system>
```

The block is wrapped in `<system>...</system>` tags. The bundled system prompt's *Message Prefix* section tells the model that exactly one such block is harness-injected at the start of every user message; the model is instructed to treat any further `<system>` tags within the same message as user-supplied content.

### Why a `SystemEphemeral` role

The ephemeral turn is a distinct `ModelRole` for two reasons:

1. **Persistence asymmetry.** The persistence handler subscribes to `agent:turn` and writes everything except `SystemEphemeral` (it's filtered at the role check; future ephemeral roles can be filtered the same way). The block changes every iteration; persisting it would freeze stale data.
2. **Provider mapping clarity.** Providers that distinguish "running system text" from "the system prompt itself" can map the two roles differently. Today both `System` and `SystemEphemeral` are wire-mapped to the API's system role; the room to refine that exists when needed.

`SystemEphemeral` is rendered as a system-class message (not a user message) because it's harness-authored context, not human input. The convention of injecting it *immediately before* the most recent user turn is what makes it function as a per-turn preface.

## Workspace files surfaced into the block

`FilesystemPromptContextProvider` reads three conventional files in this order:

```csharp
"BOOTSTRAP.md", "IDENTITY.md", "SOUL.md"
```

Each is read in full and added to `Files` as `(Name, Content)`. Missing files are silently skipped; the template iterates whatever's present.

The `AdditionalFiles` listing is every other `*.md` in the workspace root that *isn't* in the conventional set. The model gets file names only — it can issue a `read_file` tool call when it actually needs the body. This trades token cost for a discovery hop and lands on the right side of "models are good at deciding what to read; they're terrible at being told to read everything."

## Bus events from the renderer

There aren't any. The renderer is a pure function from inputs to text; the renderer doesn't publish events. The only observable behavior is the `SystemEphemeral` turn appearing in the prompt that gets sent to the model — visible in provider logs at `Trace`.

## Caching and hot reload

Both the system-prompt provider and the prompt-context provider render through `TemplateRenderer`, which caches parsed `Scriban.Template` objects through `IFileParserCache<TemplateRenderer>`. The cache key is `path + mtime + length`, so editing a template on disk invalidates the cache automatically. The TTL (`FileParserCacheOptions.TimeToLive`, default in the options class) bounds how long an unchanged template stays in memory.

There is no per-render compilation; the template is parsed once per (path, mtime) tuple and re-rendered on every call.

## What's deliberately not in the prompt

A few things you might expect:

- **Tool descriptions.** The provider serializes the available tool catalog into the wire format directly; the system prompt and ephemeral block don't enumerate tool schemas.
- **The conversation summary on a compaction.** That's a real `Assistant`-roled turn ([compaction.md](compaction.md)) — not a system block.
- **Per-channel memory.** Memory search is per-agent, not per-channel. The channel id surfaces as context but is not used as a memory filter.
- **`USER.md` / `TOOLS.md` / `MEMORY.md` / `HEARTBEAT.md`.** The agent reads these through tools when it wants to. Pulling them into the ephemeral block by default would defeat the "agent decides what's relevant" model. (The bundled `system/DEFAULT.md` does point the model at them by name.)
