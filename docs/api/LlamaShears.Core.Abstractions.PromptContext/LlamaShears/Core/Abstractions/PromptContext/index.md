# LlamaShears.Core.Abstractions.PromptContext

## Types

- [IPromptContextProvider](IPromptContextProvider.md) — Renders the per-turn ephemeral context block (the harness-injected `<system>...</system>` prefix) from a workspace template under `system/context/`, with a bundled fallback. Unlike the static system prompt this is volatile — it captures values like the current time and is re-rendered for every inference call.
- [PromptContextMemory](PromptContextMemory.md) — One memory hit surfaced to the per-turn prompt-context template ([IPromptContextProvider](IPromptContextProvider.md)). The agent reads the body from disk via the read-file tool when it actually wants the content; the template only sees the summary and score.
- [PromptContextParameters](PromptContextParameters.md) — Inputs the per-turn prompt-context template renders against. The template (Scriban) decides how to format these into the `<system>...</system>` prefix; new fields are added here rather than composed in C# so the template stays the single point of authorship.

