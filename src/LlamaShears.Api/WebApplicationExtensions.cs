using Microsoft.AspNetCore.Builder;

namespace LlamaShears.Api;

/// <summary>
/// Wires the LlamaShears API request pipeline onto a <see cref="WebApplication"/>.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the LlamaShears API pipeline. Currently a no-op placeholder.
    /// </summary>
    public static WebApplication UseApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app;
    }
}
