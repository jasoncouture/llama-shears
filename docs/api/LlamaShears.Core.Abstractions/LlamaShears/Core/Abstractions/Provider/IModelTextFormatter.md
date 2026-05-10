# LlamaShears.Core.Abstractions.Provider.IModelTextFormatter

Assembly: `LlamaShears.Core.Abstractions`

Renders a [ModelTurn](ModelTurn.md) into the textual shape a specific model
or transport expects (e.g. role-tagged transcript, chat-template form).

## Methods

### `Format`([ModelTurn](ModelTurn.md) turn)

Formats the supplied turn for transport.

#### Parameters

- `turn` — Turn to render.

#### Returns

Provider-specific textual rendering of `turn`.

