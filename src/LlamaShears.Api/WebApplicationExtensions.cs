using Microsoft.AspNetCore.Builder;

namespace LlamaShears.Api;

public static class WebApplicationExtensions
{
    public static WebApplication UseApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app;
    }
}
