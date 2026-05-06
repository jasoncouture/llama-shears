using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace LlamaShears.Api;

// Refuses new /_blazor SignalR connections once ApplicationStopping has
// been signalled. The browser's circuit retry loop otherwise opens a
// fresh WebSocket the moment it sees the existing one drop, which keeps
// ASP.NET Core's graceful-drain hanging on the new in-flight request
// until HostOptions.ShutdownTimeout. Returning 503 immediately lets the
// host exit on its own schedule. Already-upgraded WebSockets are killed
// by the host's shorter ShutdownTimeout (see AddApi).
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
