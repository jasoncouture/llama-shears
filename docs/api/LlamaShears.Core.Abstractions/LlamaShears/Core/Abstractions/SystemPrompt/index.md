# LlamaShears.Core.Abstractions.SystemPrompt

## Types

- [ISystemPromptProvider](ISystemPromptProvider.md) — Resolves the system-prompt block injected at the start of an agent interaction. Implementations look up a Scriban template by file name and render it against the supplied data bag.
- [ITemplateFileLocator](ITemplateFileLocator.md) — Resolves a template file across the standard layered lookup: per-workspace customization first, then operator-supplied templates, then the bundled defaults that ship with the host. Returns the full path of the first file that exists, or `null` if no candidate hits.
- [ITemplateRenderer](ITemplateRenderer.md) — Renders a template file against a string-keyed data bag. Implementations own the template language (today: Scriban); callers see only the rendered string. The bag is the full template input — the renderer does not resolve values itself, callers materialize whatever the template needs and hand it in.
- [WorkspaceContext](WorkspaceContext.md) — Per-agent workspace overlay: the absolute path the agent reads, writes, and persists state in, together with the workspace files loaded at scope initialization. Stashed on the data-context scope under [WorkspaceContext](WorkspaceContext.md).`DataKey` for template consumption.
- [WorkspaceFile](WorkspaceFile.md) — In-memory representation of a single file that should land in an agent's workspace overlay alongside the rendered system prompt.

