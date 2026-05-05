using System.Security.Claims;
using LlamaShears.Api.Tools.ModelContextProtocol;
using Microsoft.AspNetCore.Http;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol;

public sealed class WhoamiToolTests
{
    [Test]
    public async Task ReturnsGuestWhenNoHttpContextIsPresent()
    {
        var tool = new WhoamiTool(new TestHttpContextAccessor());

        var result = tool.Whoami();

        await Assert.That(result).IsEqualTo("guest");
    }

    [Test]
    public async Task ReturnsGuestWhenTheCallerIsNotAuthenticated()
    {
        var accessor = new TestHttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var tool = new WhoamiTool(accessor);

        var result = tool.Whoami();

        await Assert.That(result).IsEqualTo("guest");
    }

    [Test]
    public async Task ReturnsTheNameIdentifierClaimValueWhenTheCallerIsAuthenticated()
    {
        var ctx = new DefaultHttpContext();
        var identity = new ClaimsIdentity(authenticationType: "test");
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "alice"));
        ctx.User = new ClaimsPrincipal(identity);
        var tool = new WhoamiTool(new TestHttpContextAccessor { HttpContext = ctx });

        var result = tool.Whoami();

        await Assert.That(result).IsEqualTo("alice");
    }

    [Test]
    public async Task ReturnsGuestWhenAuthenticatedUserHasNoNameIdentifierClaim()
    {
        var ctx = new DefaultHttpContext();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "test"));
        var tool = new WhoamiTool(new TestHttpContextAccessor { HttpContext = ctx });

        var result = tool.Whoami();

        await Assert.That(result).IsEqualTo("guest");
    }

    private sealed class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; }
    }
}
