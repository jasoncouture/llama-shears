using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class InterruptCommand : ISlashCommand
{
    private readonly IAgentDirectory _directory;

    public InterruptCommand(IAgentDirectory directory)
    {
        _directory = directory;
    }

    public string Name => "/interrupt";

    public string Description => "Interrupts the agent's in-flight turn. Persisted context is preserved; partial assistant text or thought fragments are dropped. The agent stays live and resumes on the next inbound message.";

    public ImmutableArray<SlashCommandParameter> Parameters => [];

    public async Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken)
    {
        await _directory.InterruptAsync(context.AgentId, cancellationToken).ConfigureAwait(false);
        return SlashCommandResult.StreamingWasInterrupted;
    }
}
