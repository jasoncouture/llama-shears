# LlamaShears.Core.Abstractions.SystemPrompt.ISystemPromptProvider

Assembly: `LlamaShears.Core.Abstractions`

Resolves a named system prompt template, renders it against
[SystemPromptTemplateParameters](SystemPromptTemplateParameters.md), and returns the body
to feed into the model's
[ModelRole](../Provider/ModelRole.md).`System`
turn. Bodies are stable for the agent's lifetime so the model's
prompt-cache prefix stays warm across turns.

## Methods

### `GetAsync`(string templateName, [SystemPromptTemplateParameters](SystemPromptTemplateParameters.md) parameters, CancellationToken cancellationToken)

Resolves `templateName` to its system-prompt
body, rendered against `parameters`.
`null`, empty, or whitespace names default to
the framework's `DEFAULT` template. The name must not
contain path separators. Implementations may search multiple
roots and fall back to the framework's bundled default; throw
when no candidate exists.

