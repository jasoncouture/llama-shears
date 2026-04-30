using System.Diagnostics.CodeAnalysis;

namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// In-process store that issues opaque single-use bearer tokens bound to an
/// <see cref="AgentInfo"/>. Tokens are valid until consumed (via
/// <see cref="TryGetAgentInformation"/>) or until they expire — whichever
/// comes first.
/// </summary>
public interface IAgentTokenStore
{
    /// <summary>
    /// Issue a fresh token bound to <paramref name="agent"/>. The token is a
    /// base64-encoded opaque string. Callers must treat it as a credential.
    /// </summary>
    string Issue(AgentInfo agent);

    /// <summary>
    /// Atomically consume <paramref name="token"/>: if it is a known and
    /// unexpired token, return its bound <see cref="AgentInfo"/> and remove
    /// the entry from the store. Subsequent calls with the same token return
    /// false.
    /// </summary>
    bool TryGetAgentInformation(string token, [NotNullWhen(true)] out AgentInfo? agent);
}
