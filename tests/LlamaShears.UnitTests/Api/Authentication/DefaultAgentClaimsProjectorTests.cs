using System.Security.Claims;
using LlamaShears.Agent.Abstractions;
using LlamaShears.Api.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace LlamaShears.UnitTests.Api.Authentication;

public sealed class DefaultAgentClaimsProjectorTests
{
    [Test]
    public async Task Project_emits_a_name_identifier_claim_carrying_the_agent_id()
    {
        var projector = BuildProjector();

        var principal = projector.Project(SampleAgent("alice"), AgentBearerDefaults.AuthenticationScheme);

        await Assert.That(principal.FindFirstValue(ClaimTypes.NameIdentifier)).IsEqualTo("alice");
    }

    [Test]
    public async Task Project_emits_a_name_claim_carrying_the_agent_id()
    {
        var projector = BuildProjector();

        var principal = projector.Project(SampleAgent("alice"), AgentBearerDefaults.AuthenticationScheme);

        await Assert.That(principal.Identity!.Name).IsEqualTo("alice");
    }

    [Test]
    public async Task Project_returns_an_authenticated_identity_for_the_supplied_scheme()
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
