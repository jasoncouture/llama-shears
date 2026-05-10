# LlamaShears.Core.Abstractions.SystemPrompt.ITemplateRenderer

Assembly: `LlamaShears.Core.Abstractions.SystemPrompt`

Renders a template file against a string-keyed data bag. Implementations
own the template language (today: Scriban); callers see only the
rendered string. The bag is the full template input — the renderer
does not resolve values itself, callers materialize whatever the
template needs and hand it in.

