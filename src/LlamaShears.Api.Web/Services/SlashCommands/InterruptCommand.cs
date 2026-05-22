using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class InterruptCommand : ISlashCommand
{
    private readonly IEventBus _eventBus;

    public InterruptCommand(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public string Name => "/interrupt";

    public string Description => "Interrupts the agent's in-flight turn. Persisted context is preserved; partial assistant text or thought fragments are dropped. The agent stays live and resumes on the next inbound message.";

    public ImmutableArray<SlashCommandParameter> Parameters => [];

    [Obsolete]
    public async Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken)
    {
        await _eventBus.PublishAsync(
            Event.WellKnown.Command.InterruptAgent with { Id = context.Session },
            AgentInterruptRequest.Instance,
            cancellationToken);
        return SlashCommandResult.StreamingWasInterrupted;
    }
}
