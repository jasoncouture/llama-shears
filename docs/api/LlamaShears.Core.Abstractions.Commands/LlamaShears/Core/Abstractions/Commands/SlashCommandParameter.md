# LlamaShears.Core.Abstractions.Commands.SlashCommandParameter

Assembly: `LlamaShears.Core.Abstractions.Commands`

Declared parameter for an [ISlashCommand](ISlashCommand.md). Surfaced
through [ISlashCommand](ISlashCommand.md).`Parameters` for help / discovery.
Required parameters whose positional argument is missing should be
rejected by the command's [ISlashCommand](ISlashCommand.md).`ExecuteAsync`.

## Parameters

- `Name` — Short identifier for the parameter (no leading dash).
- `Description` — User-facing description of the parameter.
- `Required` — Whether the parameter must be supplied.

## Properties

### `Description`

User-facing description of the parameter.

### `Name`

Short identifier for the parameter (no leading dash).

### `Required`

Whether the parameter must be supplied.

## Methods

### `SlashCommandParameter`(string Name, string Description, bool Required)

Declared parameter for an [ISlashCommand](ISlashCommand.md). Surfaced
through [ISlashCommand](ISlashCommand.md).`Parameters` for help / discovery.
Required parameters whose positional argument is missing should be
rejected by the command's [ISlashCommand](ISlashCommand.md).`ExecuteAsync`.

#### Parameters

- `Name` — Short identifier for the parameter (no leading dash).
- `Description` — User-facing description of the parameter.
- `Required` — Whether the parameter must be supplied.

