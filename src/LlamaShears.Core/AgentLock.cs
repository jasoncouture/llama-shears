using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Common;

namespace LlamaShears.Core;

public sealed class AgentLock : IAgentLock
{
    private readonly IAgentLockManager _manager;
    private readonly IDataContextScope _dataScope;

    public AgentLock(IAgentLockManager manager, IDataContextScope dataScope)
    {
        _manager = manager;
        _dataScope = dataScope;
    }

    public ValueTask<ILockScope> AcquireLockAsync(CancellationToken cancellationToken)
        => _manager.AcquireLockAsync(_dataScope.GetAgentConfig().Id, cancellationToken);
}
