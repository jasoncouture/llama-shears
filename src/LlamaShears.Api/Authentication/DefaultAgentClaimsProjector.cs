using System.Security.Claims;
using LlamaShears.Agent.Abstractions;

namespace LlamaShears.Api.Authentication;

public sealed class DefaultAgentClaimsProjector : IAgentClaimsProjector
{
    public ClaimsPrincipal Project(AgentInfo agent, string authenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentException.ThrowIfNullOrWhiteSpace(authenticationScheme);

        var identity = new ClaimsIdentity(
            authenticationType: authenticationScheme,
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, agent.AgentId));
        identity.AddClaim(new Claim(ClaimTypes.Name, agent.AgentId));

        return new ClaimsPrincipal(identity);
    }
}
