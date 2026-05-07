using System.Collections.Immutable;

namespace LlamaShears.Core.Abstractions.Commands;

/// <summary>
/// Catalogue of registered <see cref="ISlashCommand"/>s. Single-instance
/// per process; populated from DI. Lookup is case-insensitive on the
/// command name.
/// </summary>
public interface ISlashCommandRegistry
{
    /// <summary>Every registered command, in registration order.</summary>
    ImmutableArray<ISlashCommand> Commands { get; }

    /// <summary>
    /// Find a command by its declared <see cref="ISlashCommand.Name"/>.
    /// Returns <see langword="null"/> when no command matches.
    /// </summary>
    ISlashCommand? Find(string name);
}
