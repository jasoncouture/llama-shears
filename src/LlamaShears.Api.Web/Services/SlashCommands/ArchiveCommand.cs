using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class ArchiveCommand : ISlashCommand
{
    private readonly IAgentDirectory _directory;

    public ArchiveCommand(IAgentDirectory directory)
    {
        _directory = directory;
    }

    public string Name => "/archive";

    public string Description => "Moves the agent's stored conversation context to a timestamped archive file and resets the chat view.";

    public ImmutableArray<SlashCommandParameter> Parameters => [];

    public async Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken)
    {
        await _directory.ClearAsync(context.Session, archive: true, cancellationToken);
        return SlashCommandResult.ContextWasChanged;
    }
}
