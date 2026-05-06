# LlamaShears.Core.Abstractions.PromptContext.IPromptContextProvider

Assembly: `LlamaShears.Core.Abstractions`

Renders the per-turn ephemeral context block (the harness-injected
`<system>...</system>` prefix) from a workspace
template under `system/context/`, with a bundled fallback.
Unlike the static system prompt this is volatile — it captures
values like the current time and is re-rendered for every
inference call.

## Methods

### `GetAsync`(string templateName, [PromptContextParameters](PromptContextParameters.md) parameters, CancellationToken cancellationToken)

Renders the prompt-context template named by
`templateName` (e.g. `"PROMPT"`) against
`parameters`. The provider looks for the
template under the workspace's `system/context/` directory
first, then the bundled fallback, falling back to the default
(`PROMPT.md`) name at each layer if the requested name is
missing. Returns `null` when nothing is found
in any candidate location. An empty rendered body is returned
as-is; callers that want to skip the injection should treat
null and empty alike.

