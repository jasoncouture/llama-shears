using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core;

internal sealed class TransientAgentFactory : ITransientAgentFactory
{
    private readonly IAgentFactory _agentFactory;
    private readonly IDataContextScope _dataScope;

    public TransientAgentFactory(IAgentFactory agentFactory, IDataContextScope dataScope)
    {
        _agentFactory = agentFactory;
        _dataScope = dataScope;
    }

    public ValueTask<AgentHandle> CreateTransientAgent(
        AgentConfig config,
        string name,
        ModelTurn initialTurn,
        IEnumerable<KeyValuePair<string, object?>> data,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(initialTurn);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (initialTurn.Role is not ModelRole.User and not ModelRole.FrameworkUser)
            throw new ArgumentException(
                $"Initial turn role must be {nameof(ModelRole.User)} but was {initialTurn.Role}.",
                nameof(initialTurn));

        var parentPath = _dataScope.GetSessionPath();
        var childPath = parentPath.CreateChildSession(new SessionId(config.Id, name));
        var enrichedData = data.Append(new KeyValuePair<string, object?>(
            TransientAgentInitialPrompt.DataKey,
            new TransientAgentInitialPrompt(initialTurn)));

        return _agentFactory.CreateAgentAsync<ITransientAgent>(
            config,
            childPath,
            enrichedData,
            cancellationToken);
    }
}
