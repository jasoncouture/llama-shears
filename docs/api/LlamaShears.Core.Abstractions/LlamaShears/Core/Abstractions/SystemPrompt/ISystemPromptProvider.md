# LlamaShears.Core.Abstractions.SystemPrompt.ISystemPromptProvider

Assembly: `LlamaShears.Core.Abstractions`

Resolves the system-prompt block injected at the start of an agent
interaction. Implementations look up a Scriban template by file name
and render it against the supplied data bag.

## Methods

### `GetAsync`(string templateName, IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken)

Renders the system prompt for the current turn.

#### Parameters

- `templateName` — File name (with extension) of the system-prompt template; `null` selects the framework default.
- `data` — Template parameters made available under their keys inside the Scriban scope.
- `cancellationToken` — Cancellation token.

#### Returns

The rendered system-prompt text.

