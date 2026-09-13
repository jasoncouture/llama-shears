# LlamaShears.Core.Abstractions.Provider.IModelPromptAssembler

Assembly: `LlamaShears.Core.Abstractions`

Prepends the framework system prompt and ephemeral context onto
caller-supplied turns. Template names come from the in-scope
[AgentConfig](../Agent/AgentConfig.md); callers overlay that config when they
need a different system or prompt-context template.

## Methods

### `AssembleAsync`(IReadOnlyList<[ModelTurn](ModelTurn.md)> turns, CancellationToken cancellationToken)

Returns a prompt of
`[System, SystemEphemeral?, ..]`.
The ephemeral turn is omitted when the prompt-context template
renders to whitespace. When present, it is inserted immediately
before the trailing user cluster (the same rule the agent
pipeline uses).

