# LlamaShears.Core.Abstractions.Provider.IModelCompletionResponse

Assembly: `LlamaShears.Core.Abstractions.Provider`

Final fragment in a model response carrying metadata about the
completed turn. Emitted exactly once, after every text and thought
fragment, so callers can react to completion-time information
(token usage, etc.) without needing a separate signal.

## Properties

### `TokenCount`

Total tokens consumed by the conversation through the end of
this turn — typically `prompt_tokens + response_tokens`.
Providers without a server-side count may return a generous
estimate; the value is intended as an upper bound for context
budgeting, never an under-count.

