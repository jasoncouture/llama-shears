using System.Security.Claims;
using LlamaShears.Agent.Abstractions;
using LlamaShears.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Api.Authentication;

public sealed class AgentBearerAuthenticationTests
{
    [Test]
    public async Task MissingAuthorizationHeaderYieldsNoResult()
    {
        var (root, _) = BuildPipeline();

        var result = await AuthenticateAsync(root, authHeader: null);

        await Assert.That(result.None).IsTrue();
    }

    [Test]
    public async Task NonBearerAuthorizationHeaderYieldsNoResult()
    {
        var (root, _) = BuildPipeline();

        var result = await AuthenticateAsync(root, authHeader: "Basic dXNlcjpwYXNz");

        await Assert.That(result.None).IsTrue();
    }

    [Test]
    public async Task BearerHeaderWithEmptyTokenFailsAuthentication()
    {
        var (root, _) = BuildPipeline();

        var result = await AuthenticateAsync(root, authHeader: "Bearer ");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.None).IsFalse();
    }

    [Test]
    public async Task BearerHeaderWithUnknownTokenFailsAuthentication()
    {
        var (root, _) = BuildPipeline();

        var result = await AuthenticateAsync(root, authHeader: "Bearer not-a-real-token");

        await Assert.That(result.Succeeded).IsFalse();
        await Assert.That(result.None).IsFalse();
    }

    [Test]
    public async Task BearerHeaderWithAValidTokenAuthenticatesWithTheAgentIdentity()
    {
        var (root, store) = BuildPipeline();
        var token = store.Issue(SampleAgent("alice"));

        var result = await AuthenticateAsync(root, authHeader: $"Bearer {token}");
        var principal = result.Principal!;
        var identity = principal.Identity!;

        await Assert.That(result.Succeeded).IsTrue();
        await Assert.That(principal.FindFirstValue(ClaimTypes.NameIdentifier)).IsEqualTo("alice");
        await Assert.That(identity.Name).IsEqualTo("alice");
        await Assert.That(identity.AuthenticationType).IsEqualTo(AgentBearerDefaults.AuthenticationScheme);
    }

    [Test]
    public async Task AValidTokenCanOnlyBeUsedOnce()
    {
        var (root, store) = BuildPipeline();
        var token = store.Issue(SampleAgent("alice"));

        var first = await AuthenticateAsync(root, authHeader: $"Bearer {token}");
        var second = await AuthenticateAsync(root, authHeader: $"Bearer {token}");

        await Assert.That(first.Succeeded).IsTrue();
        await Assert.That(second.Succeeded).IsFalse();
    }

    private static (IServiceProvider Root, IAgentTokenStore Store) BuildPipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddAgentBearerAuthentication();

        var root = services.BuildServiceProvider();
        return (root, root.GetRequiredService<IAgentTokenStore>());
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(
        IServiceProvider root,
        string? authHeader)
    {
        using var scope = root.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        if (authHeader is not null)
        {
            ctx.Request.Headers.Authorization = authHeader;
        }
        return await auth.AuthenticateAsync(ctx, AgentBearerDefaults.AuthenticationScheme);
    }

    private static AgentInfo SampleAgent(string id = "alice")
        => new(id, "ollama:llama3", 8192);
}
