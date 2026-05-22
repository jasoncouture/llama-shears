using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class CompactCommand : ISlashCommand
{
    private readonly IEventBus _eventBus;

    public CompactCommand(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public string Name => "/compact";

    public string Description => "Forces an immediate context compaction on the agent regardless of token-budget pressure.";

    public ImmutableArray<SlashCommandParameter> Parameters => [];

    public async Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            Event.WellKnown.Command.CompactionRequest with { Id = context.Session },
            AgentCompactionRequest.Forced,
            cancellationToken);
        return SlashCommandResult.Default;
    }
}
