using LlamaShears.Agent.Abstractions;
using LlamaShears.Api.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Api.Authentication;

public sealed class RejectInvalidAgentBearerMiddlewareTests
{
    [Test]
    public async Task Passes_through_when_no_authorization_header_is_supplied()
    {
        var (root, _) = BuildPipeline();

        var (status, nextCalled) = await RunAsync(root, authHeader: null);

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(status).IsEqualTo(StatusCodes.Status200OK);
    }

    [Test]
    public async Task Passes_through_when_a_non_bearer_authorization_header_is_supplied()
    {
        var (root, _) = BuildPipeline();

        var (status, nextCalled) = await RunAsync(root, authHeader: "Basic dXNlcjpwYXNz");

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(status).IsEqualTo(StatusCodes.Status200OK);
    }

    [Test]
    public async Task Passes_through_when_a_valid_bearer_token_is_supplied()
    {
        var (root, store) = BuildPipeline();
        var token = store.Issue(SampleAgent("alice"));

        var (status, nextCalled) = await RunAsync(root, authHeader: $"Bearer {token}");

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(status).IsEqualTo(StatusCodes.Status200OK);
    }

    [Test]
    public async Task Returns_403_when_an_unknown_bearer_token_is_supplied()
    {
        var (root, _) = BuildPipeline();

        var (status, nextCalled) = await RunAsync(root, authHeader: "Bearer not-a-real-token");

        await Assert.That(nextCalled).IsFalse();
        await Assert.That(status).IsEqualTo(StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task Returns_403_when_an_empty_bearer_token_is_supplied()
    {
        var (root, _) = BuildPipeline();

        var (status, nextCalled) = await RunAsync(root, authHeader: "Bearer ");

        await Assert.That(nextCalled).IsFalse();
        await Assert.That(status).IsEqualTo(StatusCodes.Status403Forbidden);
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

    private static async Task<(int Status, bool NextCalled)> RunAsync(
        IServiceProvider root,
        string? authHeader)
    {
        using var scope = root.CreateScope();
        var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        if (authHeader is not null)
        {
            ctx.Request.Headers.Authorization = authHeader;
        }

        var nextCalled = false;
        RequestDelegate next = _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var middleware = scope.ServiceProvider.GetRequiredService<RejectInvalidAgentBearerMiddleware>();
        await middleware.InvokeAsync(ctx, next);

        return (ctx.Response.StatusCode, nextCalled);
    }

    private static AgentInfo SampleAgent(string id = "alice")
        => new(id, "ollama:llama3", 8192);
}
