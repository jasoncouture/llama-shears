using Microsoft.AspNetCore.Builder;

namespace LlamaShears.Api.Authentication;

public static class RejectInvalidAgentBearerApplicationBuilderExtensions
{
    public static IApplicationBuilder UseRejectInvalidAgentBearer(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<RejectInvalidAgentBearerMiddleware>();
    }
}
