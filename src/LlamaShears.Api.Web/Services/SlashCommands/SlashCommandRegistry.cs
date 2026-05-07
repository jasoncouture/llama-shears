using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class SlashCommandRegistry : ISlashCommandRegistry
{
    private readonly ImmutableDictionary<string, ISlashCommand> _byName;

    public SlashCommandRegistry(IEnumerable<ISlashCommand> commands)
    {
        Commands = [.. commands];
        _byName = Commands.ToImmutableDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    public ImmutableArray<ISlashCommand> Commands { get; }

    public ISlashCommand? Find(string name) =>
        string.IsNullOrEmpty(name) ? null : _byName.GetValueOrDefault(name);
}
