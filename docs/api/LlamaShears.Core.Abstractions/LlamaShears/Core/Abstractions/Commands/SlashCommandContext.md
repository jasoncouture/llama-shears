# LlamaShears.Core.Abstractions.Commands.SlashCommandContext

Assembly: `LlamaShears.Core.Abstractions`

Per-invocation input for an [ISlashCommand](ISlashCommand.md). Carries the
session the command is acting on plus the positional arguments parsed
from the input line.

## Parameters

- `Session` — The session in scope for this command invocation.
- `Arguments` — Positional arguments after the command name, in input order. Empty when none were supplied.

## Properties

### `Arguments`

Positional arguments after the command name, in input order. Empty when none were supplied.

### `Session`

The session in scope for this command invocation.

## Methods

### `SlashCommandContext`([SessionId](../Agent/Sessions/SessionId.md) Session, ImmutableArray<string> Arguments)

Per-invocation input for an [ISlashCommand](ISlashCommand.md). Carries the
session the command is acting on plus the positional arguments parsed
from the input line.

#### Parameters

- `Session` — The session in scope for this command invocation.
- `Arguments` — Positional arguments after the command name, in input order. Empty when none were supplied.

