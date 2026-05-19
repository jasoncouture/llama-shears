using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentLifecycleServiceTests
{
    [Test]
    public async Task BirthSeedsWorkspaceCreatesHandleAndPublishesStartRequest()
    {
        var fixture = new Fixture();
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id: "agent-a");
        var sessionPath = new SessionPath(new SessionId(config.Id, SessionId.DefaultSessionName));
        var handle = Fixture.BuildHandle(sessionPath);

        fixture.Paths
            .GetPath(PathKind.Templates, "workspace")
            .Returns("/templates/workspace");
        fixture.Paths
            .GetPath(PathKind.Workspace, config.Id)
            .Returns("/workspaces/agent-a");
        fixture.Factory
            .CreateAgentAsync(
                Arg.Is<AgentConfig>(c => c.Id == config.Id),
                Arg.Is<SessionPath>(p => p.Current.AgentId == config.Id && p.IsRootSession),
                Arg.Any<IEnumerable<KeyValuePair<string, object?>>>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(handle));

        await fixture.Service.HandleAsync(EnvelopeFor(config), CancellationToken.None);

        fixture.Seeder.Received(1).SeedIfEmpty("/templates/workspace", "/workspaces/agent-a");
        await fixture.Bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Id == config.Id
                && t.Component == Event.WellKnown.Command.AgentStart.Component
                && t.EventName == Event.WellKnown.Command.AgentStart.EventName),
            Arg.Is<AgentStartRequest>(r => ReferenceEquals(r.Handle, handle)),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BirthWithNullPayloadIsNoOp()
    {
        var fixture = new Fixture();
        var envelope = Substitute.For<IEventEnvelope<AgentConfig>>();
        envelope.Data.Returns((AgentConfig?)null);

        await fixture.Service.HandleAsync(envelope, CancellationToken.None);

        fixture.Seeder.DidNotReceiveWithAnyArgs().SeedIfEmpty(default!, default!);
        await fixture.Factory.DidNotReceiveWithAnyArgs().CreateAgentAsync(
            default!, default(SessionPath)!, default!, default);
        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(AgentStartRequest)!, default, default);
    }

    [Test]
    public async Task BirthFactoryFailureSwallowsAndDoesNotPublishStart()
    {
        var fixture = new Fixture();
        var config = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id: "agent-a");
        fixture.Factory
            .CreateAgentAsync(
                Arg.Any<AgentConfig>(),
                Arg.Any<SessionPath>(),
                Arg.Any<IEnumerable<KeyValuePair<string, object?>>>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        await fixture.Service.HandleAsync(EnvelopeFor(config), CancellationToken.None);

        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(AgentStartRequest)!, default, default);
    }

    [Test]
    public async Task DeathWithMatchingRootPublishesStopRequest()
    {
        var fixture = new Fixture();
        var sessionPath = new SessionPath(new SessionId("agent-a", SessionId.DefaultSessionName));
        var rootHandle = Fixture.BuildHandle(sessionPath);
        fixture.Repository.GetAllAgents().Returns([rootHandle]);

        var envelope = Substitute.For<IEventEnvelope<AgentDeath>>();
        envelope.Type.Returns(Event.WellKnown.Lifecycle.Death with { Id = "agent-a" });
        envelope.Data.Returns(AgentDeath.Instance);

        await fixture.Service.HandleAsync(envelope, CancellationToken.None);

        await fixture.Bus.Received(1).PublishAsync(
            Arg.Is<EventType>(t => t.Id == "agent-a"
                && t.Component == Event.WellKnown.Command.AgentStop.Component
                && t.EventName == Event.WellKnown.Command.AgentStop.EventName),
            Arg.Is<AgentStopRequest>(r => r.SessionId == sessionPath.Current),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task DeathWithBlankAgentIdIsNoOp()
    {
        var fixture = new Fixture();
        var envelope = Substitute.For<IEventEnvelope<AgentDeath>>();
        envelope.Type.Returns(Event.WellKnown.Lifecycle.Death);
        envelope.Data.Returns(AgentDeath.Instance);

        await fixture.Service.HandleAsync(envelope, CancellationToken.None);

        fixture.Repository.DidNotReceiveWithAnyArgs().GetAllAgents();
        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(AgentStopRequest)!, default, default);
    }

    [Test]
    public async Task DeathWithNoMatchingRootIsNoOp()
    {
        var fixture = new Fixture();
        fixture.Repository.GetAllAgents().Returns([]);

        var envelope = Substitute.For<IEventEnvelope<AgentDeath>>();
        envelope.Type.Returns(Event.WellKnown.Lifecycle.Death with { Id = "agent-missing" });
        envelope.Data.Returns(AgentDeath.Instance);

        await fixture.Service.HandleAsync(envelope, CancellationToken.None);

        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(AgentStopRequest)!, default, default);
    }

    [Test]
    public async Task DeathIgnoresNonRootHandles()
    {
        var fixture = new Fixture();
        var rootPath = new SessionPath(new SessionId("agent-a", SessionId.DefaultSessionName));
        var childPath = rootPath.CreateChildSession(new SessionId("agent-a", "child"));
        var childOnly = Fixture.BuildHandle(childPath);
        fixture.Repository.GetAllAgents().Returns([childOnly]);

        var envelope = Substitute.For<IEventEnvelope<AgentDeath>>();
        envelope.Type.Returns(Event.WellKnown.Lifecycle.Death with { Id = "agent-a" });
        envelope.Data.Returns(AgentDeath.Instance);

        await fixture.Service.HandleAsync(envelope, CancellationToken.None);

        await fixture.Bus.DidNotReceiveWithAnyArgs().PublishAsync(
            default!, default(AgentStopRequest)!, default, default);
    }

    [Test]
    public async Task BirthUsesConfigWorkspacePathWhenProvided()
    {
        var fixture = new Fixture();
        var baseConfig = TestAgentConfigs.WithHeartbeat(TimeSpan.Zero, id: "agent-a");
        var config = baseConfig with { WorkspacePath = "/custom/path" };
        var sessionPath = new SessionPath(new SessionId(config.Id, SessionId.DefaultSessionName));
        fixture.Paths
            .GetPath(PathKind.Templates, "workspace")
            .Returns("/templates/workspace");
        fixture.Factory
            .CreateAgentAsync(
                Arg.Any<AgentConfig>(),
                Arg.Any<SessionPath>(),
                Arg.Any<IEnumerable<KeyValuePair<string, object?>>>(),
                Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Fixture.BuildHandle(sessionPath)));

        await fixture.Service.HandleAsync(EnvelopeFor(config), CancellationToken.None);

        fixture.Seeder.Received(1).SeedIfEmpty("/templates/workspace", "/custom/path");
        fixture.Paths.DidNotReceive().GetPath(PathKind.Workspace, Arg.Any<string>());
    }

    private static IEventEnvelope<AgentConfig> EnvelopeFor(AgentConfig config)
    {
        var envelope = Substitute.For<IEventEnvelope<AgentConfig>>();
        envelope.Type.Returns(Event.WellKnown.Lifecycle.Birth with { Id = config.Id });
        envelope.Data.Returns(config);
        return envelope;
    }

    private sealed class Fixture
    {
        public IEventBus Bus { get; } = Substitute.For<IEventBus>();
        public IAgentFactory Factory { get; } = Substitute.For<IAgentFactory>();
        public IAgentInstanceRepository Repository { get; } = Substitute.For<IAgentInstanceRepository>();
        public IApplicationPathProvider Paths { get; } = Substitute.For<IApplicationPathProvider>();
        public IDirectorySeeder Seeder { get; } = Substitute.For<IDirectorySeeder>();
        public AgentLifecycleService Service { get; }

        public Fixture()
        {
            Service = new AgentLifecycleService(
                Bus,
                Factory,
                Repository,
                Paths,
                Seeder,
                NullLogger<AgentLifecycleService>.Instance);
        }

        public static AgentHandle BuildHandle(SessionPath path)
        {
            var scope = new AsyncServiceScope(Substitute.For<IServiceScope>());
            return new AgentHandle(path, "hash", scope, ExecutionContext.Capture()!);
        }
    }
}
