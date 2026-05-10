# LlamaShears.Core.Abstractions.SystemPrompt.ITemplateRenderer

Assembly: `LlamaShears.Core.Abstractions`

Renders a template file against a string-keyed data bag. Implementations
own the template language (today: Scriban); callers see only the
rendered string. The bag is the full template input — the renderer
does not resolve values itself, callers materialize whatever the
template needs and hand it in.

## Methods

### `RenderAsync`(string templatePath, IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken)

Renders the template at `templatePath` against
`data`.

#### Parameters

- `templatePath` — Path of the template file to render.
- `data` — Template parameters made available under their keys inside the template scope.
- `cancellationToken` — Cancellation token.

#### Returns

The rendered template, or `null` when the template resolves to nothing.

