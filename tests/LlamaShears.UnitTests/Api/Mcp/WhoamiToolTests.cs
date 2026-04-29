using System.Security.Claims;
using LlamaShears.Api.Mcp;
using Microsoft.AspNetCore.Http;

namespace LlamaShears.UnitTests.Api.Mcp;

public sealed class WhoamiToolTests
{
    [Test]
    public async Task Returns_guest_when_no_HttpContext_is_present()
    {
        var tool = new WhoamiTool(new TestHttpContextAccessor());

        var result = tool.Whoami();

        await Assert.That(result).IsEqualTo("guest");
    }

    [Test]
    public async Task Returns_guest_when_the_caller_is_not_authenticated()
    {
        var accessor = new TestHttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var tool = new WhoamiTool(accessor);

        var result = tool.Whoami();

        await Assert.That(result).IsEqualTo("guest");
    }

    [Test]
    public async Task Returns_the_NameIdentifier_claim_value_when_the_caller_is_authenticated()
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
    public async Task Returns_guest_when_authenticated_user_has_no_NameIdentifier_claim()
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
