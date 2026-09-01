using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Pipeline;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Context;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Pipeline;

public sealed class CompactionMiddleware : IAgentMiddleware
{
    private readonly IContextCompactor _compactor;
    private readonly IAgentContextProvider _agentContextProvider;
    private readonly IDataContextScope _dataScope;

    public CompactionMiddleware(
        IContextCompactor compactor,
        IAgentContextProvider agentContextProvider,
        IDataContextScope dataScope)
    {
        _compactor = compactor;
        _agentContextProvider = agentContextProvider;
        _dataScope = dataScope;
    }

    /// <inheritdoc />
    public int Order => AgentMiddlewareOrder.Compaction;

    /// <inheritdoc />
    public async Task InvokeAsync(
        AgentPipelineContext context,
        AgentMiddlewareDelegate next,
        CancellationToken cancellationToken)
    {
        await next.Invoke(context, cancellationToken);
        if (context.Outcome is not { Interrupted: false })
        {
            return;
        }

        var agentId = _dataScope.GetAgentConfig().Id;
        var snapshot = await _agentContextProvider
            .CreateAgentContextAsync(_dataScope.GetCurrentSessionId(), context.ShutdownToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Agent context provider returned null for running agent '{agentId}'.");
        var prompt = new ModelPrompt([.. context.AgentContext.Turns]);
        await _compactor.CompactAsync(snapshot, prompt, force: false, context.ShutdownToken);
    }
}
