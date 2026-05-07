using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class CompactCommand : ISlashCommand
{
    private readonly IAgentDirectory _directory;

    public CompactCommand(IAgentDirectory directory)
    {
        _directory = directory;
    }

    public string Name => "/compact";

    public string Description => "Forces an immediate context compaction on the agent regardless of token-budget pressure.";

    public ImmutableArray<SlashCommandParameter> Parameters => [];

    public async Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken)
    {
        await _directory.RequestCompactionAsync(context.AgentId, cancellationToken).ConfigureAwait(false);
        return SlashCommandResult.Default;
    }
}
