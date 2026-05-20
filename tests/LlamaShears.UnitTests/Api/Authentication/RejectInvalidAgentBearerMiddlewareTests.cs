using LlamaShears.Api.Authentication;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Authentication;

public sealed class RejectInvalidAgentBearerMiddlewareTests
{
    [Test]
    public async Task PassesThroughWhenNoAuthorizationHeaderIsSupplied()
    {
        var (root, _) = BuildPipeline();

        var (status, nextCalled) = await RunAsync(root, authHeader: null);

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(status).IsEqualTo(StatusCodes.Status200OK);
    }

    [Test]
    public async Task PassesThroughWhenANonBearerAuthorizationHeaderIsSupplied()
    {
        var (root, _) = BuildPipeline();

        var (status, nextCalled) = await RunAsync(root, authHeader: "Basic dXNlcjpwYXNz");

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(status).IsEqualTo(StatusCodes.Status200OK);
    }

    [Test]
    public async Task PassesThroughWhenAValidBearerTokenIsSupplied()
    {
        var (root, store) = BuildPipeline();
        var token = store.Issue(SampleAgent());

        var (status, nextCalled) = await RunAsync(root, authHeader: $"Bearer {token}");

        await Assert.That(nextCalled).IsTrue();
        await Assert.That(status).IsEqualTo(StatusCodes.Status200OK);
    }

    [Test]
    public async Task Returns403WhenAnUnknownBearerTokenIsSupplied()
    {
        var (root, _) = BuildPipeline();

        var (status, nextCalled) = await RunAsync(root, authHeader: "Bearer not-a-real-token");

        await Assert.That(nextCalled).IsFalse();
        await Assert.That(status).IsEqualTo(StatusCodes.Status403Forbidden);
    }

    [Test]
    public async Task Returns403WhenAnEmptyBearerTokenIsSupplied()
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
        var dataContextFactory = Substitute.For<IDataContextFactory>();
        dataContextFactory.TryJoinContextScope(Arg.Any<SessionId>(), out Arg.Any<IDataContextScope?>())
            .Returns(call =>
            {
                call[1] = Substitute.For<IDataContextScope>();
                return true;
            });
        services.AddSingleton(dataContextFactory);
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
        => new AgentInfo(new SessionId(id, SessionId.DefaultSessionName), new CompositeIdentity("ollama", "llama3"), 8192);
}
