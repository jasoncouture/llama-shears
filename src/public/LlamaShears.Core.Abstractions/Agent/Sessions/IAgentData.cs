namespace LlamaShears.Core.Abstractions.Agent.Sessions;

/// <summary>
/// Marker for any value that contributes one or more entries to an agent's per-turn data scope.
/// Consumers (e.g. <c>IAgentFactory</c>) call <see cref="GetData"/> and overlay the entries onto the
/// scope's keyed dictionary.
/// </summary>
public interface IAgentData
{
    /// <summary>Returns the key/value pairs this instance contributes to the agent data scope.</summary>
    IEnumerable<KeyValuePair<string, object?>> GetData();
}
