# LlamaShears.Core.Abstractions.PromptContext.PromptContextParameters

Assembly: `LlamaShears.Core.Abstractions.PromptContext`

Inputs the per-turn prompt-context template renders against. The
template (Scriban) decides how to format these into the
`<system>...</system>` prefix; new fields are added
here rather than composed in C# so the template stays the single
point of authorship.

## Parameters

- `Now` — Current wall-clock time formatted by the host (string so the template renders verbatim).
- `Timezone` — Host timezone display name.
- `DayOfWeek` — Current day of week as text.
- `ChannelId` — Channel correlation id when the turn originates from a channel; `null` otherwise.
- `ImportantMessage` — Optional one-shot system message to surface (e.g. a warning).
- `WorkspacePath` — The agent's workspace path; `null` when unbound.

## Properties

### `ChannelId`

Channel correlation id when the turn originates from a channel; `null` otherwise.

### `DayOfWeek`

Current day of week as text.

### `ImportantMessage`

Optional one-shot system message to surface (e.g. a warning).

### `Memories`

Memory hits surfaced to the template (typically prefetch results).

### `Now`

Current wall-clock time formatted by the host (string so the template renders verbatim).

### `Timezone`

Host timezone display name.

### `WorkspacePath`

The agent's workspace path; `null` when unbound.

## Methods

### `PromptContextParameters`(string Now, string Timezone, string DayOfWeek, string ChannelId, string ImportantMessage, string WorkspacePath)

Inputs the per-turn prompt-context template renders against. The
template (Scriban) decides how to format these into the
`<system>...</system>` prefix; new fields are added
here rather than composed in C# so the template stays the single
point of authorship.

#### Parameters

- `Now` — Current wall-clock time formatted by the host (string so the template renders verbatim).
- `Timezone` — Host timezone display name.
- `DayOfWeek` — Current day of week as text.
- `ChannelId` — Channel correlation id when the turn originates from a channel; `null` otherwise.
- `ImportantMessage` — Optional one-shot system message to surface (e.g. a warning).
- `WorkspacePath` — The agent's workspace path; `null` when unbound.

