using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Core.Abstractions.Agent;



/// <summary>
/// Context data for a transient agent
/// </summary>
/// <param name="Prompt">The initial model turn to send to the agent upon startup</param>
public sealed record TransientAgentInitialPrompt(ModelTurn Prompt)
{
    /// <summary>
    /// Data scope context key for storing and retrieving
    /// </summary>
    public const string DataKey = "transient_agent_data";
}
