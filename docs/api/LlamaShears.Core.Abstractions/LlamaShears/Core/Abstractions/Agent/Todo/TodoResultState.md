# LlamaShears.Core.Abstractions.Agent.Todo.TodoResultState

Assembly: `LlamaShears.Core.Abstractions`

Overall outcome reported by a todo command.

## Fields

### `Corrupt`

Underlying todo store was unreadable / malformed and a fresh empty list replaced it before the command applied.

### `Refused`

Command was rejected (e.g. validation failure) and the persisted state is unchanged.

### `Success`

Command applied normally and the returned items reflect the persisted state.

