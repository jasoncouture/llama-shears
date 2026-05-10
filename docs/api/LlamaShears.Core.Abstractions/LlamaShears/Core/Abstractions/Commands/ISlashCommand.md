# LlamaShears.Core.Abstractions.Commands.ISlashCommand

Assembly: `LlamaShears.Core.Abstractions`

One slash command. Implementations are registered via DI; the host
enumerates them through [ISlashCommandRegistry](ISlashCommandRegistry.md) and
dispatches by [ISlashCommand](ISlashCommand.md).`Name`.

## Properties

### `Description`

One-line user-facing description of what this command does. Surfaced in help / discovery.

### `Name`

The token entered by the user, including the leading slash (e.g. `/clear`). Lookup is case-insensitive.

### `Parameters`

Declared parameters; an empty list means the command takes no arguments.

## Methods

### `ExecuteAsync`([SlashCommandContext](SlashCommandContext.md) context, CancellationToken cancellationToken)

Executes the command. The result tells the dispatcher which downstream effects (if any) follow.

