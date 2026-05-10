# LlamaShears.Core.Abstractions.SystemPrompt

## Types

- [ITemplateFileLocator](ITemplateFileLocator.md) — Resolves a template file across the standard layered lookup: per-workspace customization first, then operator-supplied templates, then the bundled defaults that ship with the host. Returns the full path of the first file that exists, or `null` if no candidate hits.
- [ITemplateRenderer](ITemplateRenderer.md) — Renders a template file against a string-keyed data bag. Implementations own the template language (today: Scriban); callers see only the rendered string. The bag is the full template input — the renderer does not resolve values itself, callers materialize whatever the template needs and hand it in.

