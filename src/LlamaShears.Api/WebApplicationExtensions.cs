using LlamaShears.Api.Authentication;
using LlamaShears.Api.Web;
using Microsoft.AspNetCore.Builder;

namespace LlamaShears.Api;

public static class WebApplicationExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Run before the Blazor SignalR endpoint binds so a reconnect
        // request during ApplicationStopping is rejected outright instead
        // of latching onto a WebSocket and holding the host alive.
        app.UseMiddleware<RejectBlazorReconnectsAtShutdownMiddleware>();

        app.UseAuthentication();
        app.UseRejectInvalidAgentBearer();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        app.MapMcp("/mcp");

        return app;
    }
}
