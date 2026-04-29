using Microsoft.AspNetCore.Builder;

namespace LlamaShears.Api;

public static class WebApplicationExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapMcp("/mcp");

        return app;
    }
}
