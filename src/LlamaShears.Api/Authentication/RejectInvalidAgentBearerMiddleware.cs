using System.Security.Claims;
using LlamaShears.Core.Abstractions.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace LlamaShears.Api.Authentication;

public sealed class RejectInvalidAgentBearerMiddleware : IMiddleware
{
    private readonly IDataContextFactory _dataContextFactory;

    public RejectInvalidAgentBearerMiddleware(IDataContextFactory dataContextFactory)
    {
        _dataContextFactory = dataContextFactory;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var result = await context.AuthenticateAsync(AgentBearerDefaults.AuthenticationScheme);
        if (result.Failure is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var agentId = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(agentId))
        {
            _dataContextFactory.TryJoinContextScope(agentId, out _);
        }

        await next.Invoke(context);
    }
}
