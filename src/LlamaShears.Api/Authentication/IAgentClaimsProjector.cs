using System.Security.Claims;
using LlamaShears.Agent.Abstractions;

namespace LlamaShears.Api.Authentication;

/// <summary>
/// Projects an <see cref="AgentInfo"/> recovered from a consumed bearer
/// token into the <see cref="ClaimsPrincipal"/> the rest of the pipeline
/// reads from <c>HttpContext.User</c>.
/// </summary>
public interface IAgentClaimsProjector
{
    ClaimsPrincipal Project(AgentInfo agent, string authenticationScheme);
}
