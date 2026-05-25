using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// 
/// </summary>
public interface IPromptedAgentSpawner
{
    /// <summary>
    /// </summary>
    /// <param name="startInfo"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<AgentHandle> CreateAsync(PromptedAgentStartInformation startInfo, CancellationToken cancellationToken);
}
