using System.Text.Json.Serialization;

namespace LlamaShears.Core.Abstractions.Provider;

/// <summary>
/// Persisted entry recording the cumulative model token usage observed
/// at a point in the conversation. Read by
/// <see cref="LlamaShears.Core.Abstractions.Agent.Persistence.IAgentContext.TokenCount"/>
/// to surface the latest value without re-counting.
/// </summary>
/// <param name="TokenCount">Cumulative token count reported by the model at the time the entry was appended.</param>
public sealed record ModelTokenInformationContextEntry(int TokenCount) : IContextEntry
{

}
