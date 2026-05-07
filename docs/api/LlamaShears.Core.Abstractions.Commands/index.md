# LlamaShears.Core.Abstractions.Commands

Slash-command contracts for [LlamaShears](https://github.com/jasoncouture/llama-shears). Hosts and plugins both consume this package: the host's chat surface dispatches commands by name, and any plugin that wants to expose a new `/whatever` command implements `ISlashCommand` and registers it in DI.

## Public surface

- **`ISlashCommand`** — one slash command (name, description, parameters, executor).
- **`ISlashCommandRegistry`** — DI-populated catalogue; lookup by name (case-insensitive), enumerate all.
- **`SlashCommandParameter`** — declared parameter metadata (name, description, required).
- **`SlashCommandContext`** — per-invocation payload (agent id + positional arguments).
- **`SlashCommandResult`** — execution outcome with post-execute hints (e.g. `ContextChanged`).

## See also

- [LlamaShears on GitHub](https://github.com/jasoncouture/llama-shears)

## Licensing

[AGPL-3.0-or-later](https://github.com/jasoncouture/llama-shears/blob/main/LICENSE.md). [Commercial licensing](https://github.com/jasoncouture/llama-shears/blob/main/COMMERCIAL.md) is available.

---

## LlamaShears.Core.Abstractions.Commands

- [ISlashCommand](LlamaShears/Core/Abstractions/Commands/ISlashCommand.md)
- [ISlashCommandRegistry](LlamaShears/Core/Abstractions/Commands/ISlashCommandRegistry.md)
- [SlashCommandContext](LlamaShears/Core/Abstractions/Commands/SlashCommandContext.md)
- [SlashCommandParameter](LlamaShears/Core/Abstractions/Commands/SlashCommandParameter.md)
- [SlashCommandResult](LlamaShears/Core/Abstractions/Commands/SlashCommandResult.md)

