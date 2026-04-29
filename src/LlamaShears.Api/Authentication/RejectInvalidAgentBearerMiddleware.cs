using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace LlamaShears.Api.Authentication;

/// <summary>
/// Pipeline gate that turns "tried to authenticate and failed" into a
/// 403. A missing or non-bearer Authorization header is left to flow
/// through anonymously; only an explicit bearer that the
/// <see cref="AgentBearerHandler"/> rejected short-circuits the request.
/// Bug surface: the only realistic origin of a Failure result here is a
/// bug in token minting or attachment, so we prefer a loud 403 over a
/// silent fall-through to anonymous.
/// </summary>
public sealed class RejectInvalidAgentBearerMiddleware
{
    private readonly RequestDelegate _next;

    public RejectInvalidAgentBearerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var result = await context.AuthenticateAsync(AgentBearerDefaults.AuthenticationScheme);
        if (result.Failure is not null)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
