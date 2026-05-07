# LlamaShears.Core.Abstractions.Commands.ISlashCommandRegistry

Assembly: `LlamaShears.Core.Abstractions.Commands`

Catalogue of registered [ISlashCommand](ISlashCommand.md)s. Single-instance
per process; populated from DI. Lookup is case-insensitive on the
command name.

## Properties

### `Commands`

Every registered command, in registration order.

## Methods

### `Find`(string name)

Find a command by its declared [ISlashCommand](ISlashCommand.md).`Name`.
Returns `null` when no command matches.

