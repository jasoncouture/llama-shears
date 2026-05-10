# LlamaShears.Core.Abstractions.PromptContext

## Types

- [IPromptContextProvider](IPromptContextProvider.md) — Resolves the per-turn prompt-context block that is rendered alongside the system prompt. Implementations look up a Scriban template by name and render it against the supplied data bag.
- [PromptContextMemory](PromptContextMemory.md) — One memory hit surfaced to the per-turn prompt-context template ([IPromptContextProvider](IPromptContextProvider.md)). The agent reads the body from disk via the read-file tool when it actually wants the content; the template only sees the summary and score.

