# LlamaShears.Core.Abstractions.PromptContext

## Types

- [AgentSkills](AgentSkills.md) — Immutable snapshot of the skills exposed to a single agent for a single turn. Produced by [ISkillRepository](ISkillRepository.md).`GetSkillsAsync` after filtering, and surfaced through the prompt-context data bag under [AgentSkills](AgentSkills.md).`DataKey`.
- [IPromptContextProvider](IPromptContextProvider.md) — Resolves the per-turn prompt-context block that is rendered alongside the system prompt. Implementations look up a Scriban template by name and render it against the supplied data bag.
- [ISkillFilter](ISkillFilter.md) — Gatekeeps skill availability for the current agent context. The repository consults the filter before enumerating skills ([ISkillFilter](ISkillFilter.md).`AreSkillsAllowed`) and again per skill ([ISkillFilter](ISkillFilter.md).`IsSkillAllowed`) so policies can be applied globally or per name.
- [ISkillParser](ISkillParser.md) — Parses the raw text of a `SKILL.md` document into a [SkillRecord](SkillRecord.md). Implementations are pure CPU work — markdown + YAML frontmatter parsing — and do not perform I/O. Returns `null` when the document has no frontmatter; throws when the frontmatter is present but malformed (missing required keys, wrong value types, etc.) so the caller can log and skip the offending file.
- [ISkillRepository](ISkillRepository.md) — Loads the set of skills visible to a particular agent, applying any configured [ISkillFilter](ISkillFilter.md) before the result is returned. Skills are discovered on disk under the host-defined skill roots (global, app, and per-agent workspace tiers) and represented as [SkillRecord](SkillRecord.md) values.
- [PromptContextMemory](PromptContextMemory.md) — One memory hit surfaced to the per-turn prompt-context template ([IPromptContextProvider](IPromptContextProvider.md)). The agent reads the body from disk via the read-file tool when it actually wants the content; the template only sees the summary and score.
- [SkillRecord](SkillRecord.md) — Materialised skill loaded from a `SKILL.md` document. Captures the frontmatter contract (name, description, optional metadata, free-form extras) together with the resolved on-disk path and the rendered markdown body.

