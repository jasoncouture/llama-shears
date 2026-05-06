using System.Security.Claims;
using LlamaShears.Core.Abstractions.Agent;

namespace LlamaShears.Api.Authentication;

/// <summary>
/// Projects an <see cref="AgentInfo"/> recovered from a consumed bearer
/// token into the <see cref="ClaimsPrincipal"/> the rest of the pipeline
/// reads from <c>HttpContext.User</c>.
/// </summary>
public interface IAgentClaimsProjector
{
    /// <summary>
    /// Builds the <see cref="ClaimsPrincipal"/> that represents
    /// <paramref name="agent"/> for downstream authorization, scoped to
    /// <paramref name="authenticationScheme"/>.
    /// </summary>
    ClaimsPrincipal Project(AgentInfo agent, string authenticationScheme);
}
