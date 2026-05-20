using System.Security.Claims;
using LlamaShears.Core.Abstractions.Agent.Sessions;
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
        var result = await context.AuthenticateAsync(AgentBearerDefaults.AuthenticationScheme);
        if (result.Failure is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var sessionCanonical = result.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(sessionCanonical) && SessionId.TryParse(sessionCanonical, out var session))
        {
            if(!_dataContextFactory.TryJoinContextScope(session, out _))
            {
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                return;
            }
        }

        await next.Invoke(context);
    }
}
