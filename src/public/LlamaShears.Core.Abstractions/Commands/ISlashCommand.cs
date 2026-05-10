using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Commands;

/// <summary>
/// One slash command. Implementations are registered via DI; the host
/// enumerates them through <see cref="ISlashCommandRegistry"/> and
/// dispatches by <see cref="Name"/>.
/// </summary>
public interface ISlashCommand
{
    /// <summary>The token entered by the user, including the leading slash (e.g. <c>/clear</c>). Lookup is case-insensitive.</summary>
    string Name { get; }

    /// <summary>One-line user-facing description of what this command does. Surfaced in help / discovery.</summary>
    string Description { get; }

    /// <summary>Declared parameters; an empty list means the command takes no arguments.</summary>
    ImmutableArray<SlashCommandParameter> Parameters { get; }

    /// <summary>Executes the command. The result tells the dispatcher which downstream effects (if any) follow.</summary>
    Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken);
}
