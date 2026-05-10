# LlamaShears.Core.Abstractions.Commands.SlashCommandContext

Assembly: `LlamaShears.Core.Abstractions`

Per-invocation input for an [ISlashCommand](ISlashCommand.md). Carries the
agent the command is acting on plus the positional arguments parsed
from the input line.

## Parameters

- `AgentId` — The agent in scope for this command invocation.
- `Arguments` — Positional arguments after the command name, in input order. Empty when none were supplied.

## Properties

### `AgentId`

The agent in scope for this command invocation.

### `Arguments`

Positional arguments after the command name, in input order. Empty when none were supplied.

## Methods

### `SlashCommandContext`(string AgentId, ImmutableArray<string> Arguments)

Per-invocation input for an [ISlashCommand](ISlashCommand.md). Carries the
agent the command is acting on plus the positional arguments parsed
from the input line.

#### Parameters

- `AgentId` — The agent in scope for this command invocation.
- `Arguments` — Positional arguments after the command name, in input order. Empty when none were supplied.

