namespace LlamaShears.Core.Abstractions.Commands;

/// <summary>
/// Declared parameter for an <see cref="ISlashCommand"/>. Surfaced
/// through <see cref="ISlashCommand.Parameters"/> for help / discovery.
/// Required parameters whose positional argument is missing should be
/// rejected by the command's <see cref="ISlashCommand.ExecuteAsync"/>.
/// </summary>
/// <param name="Name">Short identifier for the parameter (no leading dash).</param>
/// <param name="Description">User-facing description of the parameter.</param>
/// <param name="Required">Whether the parameter must be supplied.</param>
public sealed record SlashCommandParameter(
    string Name,
    string Description,
    bool Required = true);
