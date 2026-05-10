# LlamaShears.Core.Abstractions.PromptContext.IPromptContextProvider

Assembly: `LlamaShears.Core.Abstractions`

Resolves the per-turn prompt-context block that is rendered alongside the
system prompt. Implementations look up a Scriban template by name and
render it against the supplied data bag.

## Methods

### `GetAsync`(string templateName, IReadOnlyDictionary<string, object> data, CancellationToken cancellationToken)

Renders the prompt-context template for the current turn.

#### Parameters

- `templateName` — Name of the prompt-context template; `null` selects the framework default.
- `data` — Template parameters made available under their keys inside the Scriban scope.
- `cancellationToken` — Cancellation token.

#### Returns

The rendered prompt-context block, or `null` when the template resolves to nothing.

