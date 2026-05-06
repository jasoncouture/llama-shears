using System.Net.Http.Headers;
using System.Text.Encodings.Web;
using LlamaShears.Core.Abstractions.Agent;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace LlamaShears.Api.Authentication;

public sealed class AgentBearerHandler : AuthenticationHandler<AgentBearerOptions>
{
    private const string BearerScheme = "Bearer";

    private readonly IAgentTokenStore _tokenStore;
    private readonly IAgentClaimsProjector _projector;

    public AgentBearerHandler(
        IOptionsMonitor<AgentBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IAgentTokenStore tokenStore,
        IAgentClaimsProjector projector)
        : base(options, logger, encoder)
    {
        _tokenStore = tokenStore;
        _projector = projector;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers[HeaderNames.Authorization].FirstOrDefault();
        if (string.IsNullOrEmpty(header))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!AuthenticationHeaderValue.TryParse(header, out var parsed)
            || !BearerScheme.Equals(parsed.Scheme, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var token = parsed.Parameter?.Trim();
        if (string.IsNullOrEmpty(token))
        {
            return Task.FromResult(AuthenticateResult.Fail("Empty bearer token."));
        }

        if (!_tokenStore.TryGetAgentInformation(token, out var agent))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown or expired bearer token."));
        }

        var principal = _projector.Project(agent, Scheme.Name);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
