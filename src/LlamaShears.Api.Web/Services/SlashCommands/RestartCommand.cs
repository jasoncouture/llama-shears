using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Commands;
using LlamaShears.Hosting;

namespace LlamaShears.Api.Web.Services.SlashCommands;

public sealed class RestartCommand : ISlashCommand
{
    private readonly IHostRestarter _restarter;

    public RestartCommand(IHostRestarter restarter)
    {
        _restarter = restarter;
    }

    public string Name => "/restart";

    public string Description => "Gracefully restarts the host. Re-executes the entrypoint when running outside a container; in a container, exits non-zero so the supervisor brings the process back.";

    public ImmutableArray<SlashCommandParameter> Parameters => [];

    public Task<SlashCommandResult> ExecuteAsync(SlashCommandContext context, CancellationToken cancellationToken)
    {
        _restarter.RequestRestart();
        return Task.FromResult(SlashCommandResult.Default);
    }
}
