using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace LlamaShears.Api.Authentication;

public sealed class RejectInvalidAgentBearerMiddleware : IMiddleware
{
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

        await next.Invoke(context);
    }
}
