using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace LlamaShears.Api.Mcp;

[McpServerToolType]
public sealed class WhoamiTool
{
    private const string GuestIdentity = "guest";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public WhoamiTool(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    [McpServerTool(Name = "whoami")]
    [Description("Returns the calling agent's identity, or 'guest' if the caller is not authenticated.")]
    public string Whoami()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return GuestIdentity;
        }

        return user.FindFirstValue(ClaimTypes.NameIdentifier) ?? GuestIdentity;
    }
}
