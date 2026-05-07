# LlamaShears.Core.Abstractions.SystemPrompt.ITemplateRenderer

Assembly: `LlamaShears.Core.Abstractions.SystemPrompt`

Renders a template file against an input object. Implementations
own the template language (today: Scriban); callers see only the
rendered string.

## Methods

### `RenderAsync`(string templatePath, object input, CancellationToken cancellationToken)

Reads the template at `templatePath`, binds it
to `input`, and returns the rendered output.
Returns `null` when no file exists at
`templatePath`; callers handle missing
templates as part of normal control flow rather than via
exceptions.

