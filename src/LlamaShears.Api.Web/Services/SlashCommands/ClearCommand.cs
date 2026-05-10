using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class ClearCommand : ISlashCommand
{
    private readonly IAgentDirectory _directory;

    public ClearCommand(IAgentDirectory directory)
    {
        _directory = directory;
    }

    public string Name => "/clear";

    public string Description => "Discards the agent's stored conversation context (no archive) and resets the chat view.";

    public ImmutableArray<SlashCommandParameter> Parameters => [];

    public async Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken)
    {
        await _directory.ClearAsync(context.AgentId, archive: false, cancellationToken);
        return SlashCommandResult.ContextWasChanged;
    }
}
