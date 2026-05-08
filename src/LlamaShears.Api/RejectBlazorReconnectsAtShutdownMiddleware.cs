using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Api;

public sealed class RejectBlazorReconnectsAtShutdownMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostApplicationLifetime _lifetime;

    public RejectBlazorReconnectsAtShutdownMiddleware(
        RequestDelegate next,
        IHostApplicationLifetime lifetime)
    {
        _next = next;
        _lifetime = lifetime;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (_lifetime.ApplicationStopping.IsCancellationRequested
            && context.Request.Path.StartsWithSegments("/_blazor"))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return Task.CompletedTask;
        }
        return _next.Invoke(context);
    }
}
