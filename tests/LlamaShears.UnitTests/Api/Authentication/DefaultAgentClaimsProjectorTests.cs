using System.Security.Claims;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Api.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Api.Authentication;

public sealed class DefaultAgentClaimsProjectorTests
{
    [Test]
    public async Task ProjectEmitsANameIdentifierClaimCarryingTheAgentId()
    {
        var projector = BuildProjector();

        var principal = projector.Project(SampleAgent("alice"), AgentBearerDefaults.AuthenticationScheme);

        await Assert.That(principal.FindFirstValue(ClaimTypes.NameIdentifier)).IsEqualTo("alice");
    }

    [Test]
    public async Task ProjectEmitsANameClaimCarryingTheAgentId()
    {
        var projector = BuildProjector();

        var principal = projector.Project(SampleAgent("alice"), AgentBearerDefaults.AuthenticationScheme);

        await Assert.That(principal.Identity!.Name).IsEqualTo("alice");
    }

    [Test]
    public async Task ProjectReturnsAnAuthenticatedIdentityForTheSuppliedScheme()
    {
        var projector = BuildProjector();

        var principal = projector.Project(SampleAgent(), AgentBearerDefaults.AuthenticationScheme);

        await Assert.That(principal.Identity!.IsAuthenticated).IsTrue();
        await Assert.That(principal.Identity.AuthenticationType).IsEqualTo(AgentBearerDefaults.AuthenticationScheme);
    }

    private static IAgentClaimsProjector BuildProjector()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FakeTimeProvider());
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddAgentBearerAuthentication();
        return services.BuildServiceProvider().GetRequiredService<IAgentClaimsProjector>();
    }

    private static AgentInfo SampleAgent(string id = "alice")
        => new(id, "ollama:llama3", 8192);
}
