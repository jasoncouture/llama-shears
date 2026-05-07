# LlamaShears.Core.Abstractions.Commands

## Types

- [ISlashCommand](ISlashCommand.md) — One slash command. Implementations are registered via DI; the host enumerates them through [ISlashCommandRegistry](ISlashCommandRegistry.md) and dispatches by [ISlashCommand](ISlashCommand.md).`Name`.
- [ISlashCommandRegistry](ISlashCommandRegistry.md) — Catalogue of registered [ISlashCommand](ISlashCommand.md)s. Single-instance per process; populated from DI. Lookup is case-insensitive on the command name.
- [SlashCommandContext](SlashCommandContext.md) — Per-invocation input for an [ISlashCommand](ISlashCommand.md). Carries the agent the command is acting on plus the positional arguments parsed from the input line.
- [SlashCommandParameter](SlashCommandParameter.md) — Declared parameter for an [ISlashCommand](ISlashCommand.md). Surfaced through [ISlashCommand](ISlashCommand.md).`Parameters` for help / discovery. Required parameters whose positional argument is missing should be rejected by the command's [ISlashCommand](ISlashCommand.md).`ExecuteAsync`.
- [SlashCommandResult](SlashCommandResult.md) — Outcome of an [ISlashCommand](ISlashCommand.md) execution. The flags here are post-execution hints to the dispatcher (e.g. the chat UI) so it can apply downstream effects without the command coupling to UI concerns directly.

