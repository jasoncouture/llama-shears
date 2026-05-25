using System.Text.Json;
using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Persistence;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Events;
using LlamaShears.Core.Abstractions.Events.Agent;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Core.Abstractions.SystemPrompt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace LlamaShears.UnitTests.Agent.Core;

public sealed class AgentHeartbeatServiceTests
{
    [Test]
    public async Task SecondHeartbeatBriefsOnlyParentTurnsSincePreviousHeartbeat()
    {
        var fixture = new Fixture();
        await fixture.Service.StartAsync(CancellationToken.None);

        fixture.TimeProvider.Advance(TimeSpan.FromSeconds(30));
        var preFirst = new ModelTurn(ModelRole.User, "first-turn", fixture.TimeProvider.GetLocalNow());
        await fixture.ParentContext.AppendAsync(preFirst, CancellationToken.None);

        fixture.TimeProvider.Advance(fixture.HeartbeatPeriod);
        await fixture.Service.HandleAsync(BuildTick(fixture.TimeProvider.GetLocalNow()), CancellationToken.None);

        var firstHeartbeatSession = fixture.LastHeartbeatSession;
        await Assert.That(firstHeartbeatSession).IsNotNull();
        var firstBriefing = SingleBriefing(fixture.HeartbeatContextFor(firstHeartbeatSession!));
        await Assert.That(BriefingCount(firstBriefing)).IsEqualTo(1);
        await Assert.That(firstBriefing.Content).Contains("first-turn");

        await fixture.Service.HandleAsync(
            BuildStopping(firstHeartbeatSession!, fixture.Config),
            CancellationToken.None);

        fixture.TimeProvider.Advance(TimeSpan.FromSeconds(30));
        var preSecond = new ModelTurn(ModelRole.User, "second-turn", fixture.TimeProvider.GetLocalNow());
        await fixture.ParentContext.AppendAsync(preSecond, CancellationToken.None);

        fixture.TimeProvider.Advance(fixture.HeartbeatPeriod);
        await fixture.Service.HandleAsync(BuildTick(fixture.TimeProvider.GetLocalNow()), CancellationToken.None);

        var secondHeartbeatSession = fixture.LastHeartbeatSession;
        await Assert.That(secondHeartbeatSession).IsNotNull();
        await Assert.That(secondHeartbeatSession).IsNotEqualTo(firstHeartbeatSession);

        var secondBriefing = SingleBriefing(fixture.HeartbeatContextFor(secondHeartbeatSession!));
        await Assert.That(BriefingCount(secondBriefing)).IsEqualTo(1);
        await Assert.That(secondBriefing.Content).Contains("second-turn");
        await Assert.That(secondBriefing.Content).DoesNotContain("first-turn");
    }

    private static ModelTurn SingleBriefing(FakeAgentContext context) =>
        context.Turns.Single(turn => turn.Role == ModelRole.SystemEphemeral);

    private static int BriefingCount(ModelTurn briefing)
    {
        var headerLine = briefing.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0];
        using var document = JsonDocument.Parse(headerLine);
        return document.RootElement.GetProperty("count").GetInt32();
    }

    private static IEventEnvelope<SystemTick> BuildTick(DateTimeOffset at)
    {
        var envelope = Substitute.For<IEventEnvelope<SystemTick>>();
        envelope.Type.Returns(Event.WellKnown.Host.Tick);
        envelope.Data.Returns(new SystemTick(at));
        return envelope;
    }

    private static IEventEnvelope<AgentLifecycleEvent> BuildStopping(SessionId sessionId, AgentConfig config)
    {
        var envelope = Substitute.For<IEventEnvelope<AgentLifecycleEvent>>();
        envelope.Type.Returns(Event.WellKnown.Agent.Stopping with { Id = sessionId.AgentId });
        envelope.Data.Returns(new AgentLifecycleEvent(config, sessionId));
        return envelope;
    }

    private sealed class Fixture
    {
        public TimeSpan HeartbeatPeriod { get; } = TimeSpan.FromMinutes(5);
        public FakeTimeProvider TimeProvider { get; }
        public AgentConfig Config { get; }
        public SessionId ParentSession { get; }
        public FakeAgentContext ParentContext { get; }
        public IContextStore ContextStore { get; }
        public ITransientAgentFactory TransientAgentFactory { get; }
        public IEventBus EventBus { get; } = Substitute.For<IEventBus>();
        public IFileParserCache<AgentHeartbeatService> FileParserCache { get; }
        public IApplicationPathProvider Paths { get; }
        public AgentHeartbeatService Service { get; }

        public SessionId? LastHeartbeatSession { get; private set; }

        private readonly Dictionary<SessionId, FakeAgentContext> _heartbeatContexts = [];

        public FakeAgentContext HeartbeatContextFor(SessionId session) => _heartbeatContexts[session];

        public Fixture()
        {
            TimeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
            Config = TestAgentConfigs.WithHeartbeat(HeartbeatPeriod, id: "agent-a");
            ParentSession = new SessionId(Config.Id, SessionId.DefaultSessionName);
            ParentContext = new FakeAgentContext(Config.Id);

            IDataContextScope scope = new FakeDataContextScope(ParentSession);
            scope.SetItem(AgentConfig.DataKey, Config);
            scope.SetItem(SessionPath.DataKey, new SessionPath(ParentSession));

            ContextStore = Substitute.For<IContextStore>();
            ContextStore.OpenAsync(ParentSession, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<IAgentContext>(ParentContext));
            ContextStore.OpenAsync(Arg.Is<SessionId>(s => s != ParentSession), Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var sessionId = (SessionId)call[0]!;
                    if (!_heartbeatContexts.TryGetValue(sessionId, out var context))
                    {
                        context = new FakeAgentContext(sessionId.AgentId);
                        _heartbeatContexts[sessionId] = context;
                    }
                    return Task.FromResult<IAgentContext>(context);
                });

            TransientAgentFactory = Substitute.For<ITransientAgentFactory>();
            TransientAgentFactory.CreateTransientAgent(
                    Arg.Any<AgentConfig>(),
                    Arg.Any<string>(),
                    Arg.Any<ModelTurn>(),
                    Arg.Any<IEnumerable<KeyValuePair<string, object?>>>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var name = (string)call[1]!;
                    var child = new SessionId(Config.Id, name);
                    var parentPath = new SessionPath(ParentSession);
                    var path = parentPath.CreateChildSession(child);
                    LastHeartbeatSession = child;
                    var serviceScope = new AsyncServiceScope(Substitute.For<IServiceScope>());
                    var handle = new AgentHandle(path, "hash", serviceScope, ExecutionContext.Capture()!, typeof(IAgent));
                    return ValueTask.FromResult(handle);
                });

            FileParserCache = Substitute.For<IFileParserCache<AgentHeartbeatService>>();
            FileParserCache.GetOrParseAsync<WorkspaceFile, string>(
                    Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<Func<Stream?, string, CancellationToken, ValueTask<WorkspaceFile?>>>(),
                    Arg.Any<CancellationToken>())
                .Returns(ValueTask.FromResult<WorkspaceFile?>(
                    new WorkspaceFile("HEARTBEAT.md", "/workspace/agent-a/", "do work")));

            Paths = Substitute.For<IApplicationPathProvider>();
            Paths.GetPath(PathKind.Workspace, Arg.Any<string>(), Arg.Any<bool>())
                .Returns("/workspace/agent-a/HEARTBEAT.md");

            PromptedAgentSpawner = Substitute.For<IPromptedAgentSpawner>();
            PromptedAgentSpawner.CreateAsync(Arg.Any<PromptedAgentStartInformation>(), Arg.Any<CancellationToken>())
                .Returns(call => SpawnAsync((PromptedAgentStartInformation)call[0]!, (CancellationToken)call[1]!));

            async ValueTask<AgentHandle> SpawnAsync(PromptedAgentStartInformation info, CancellationToken ct)
            {
                var handle = await TransientAgentFactory.CreateTransientAgent(
                    info.Config,
                    info.Id.Name,
                    info.InitialPrompt with { ChannelId = $"subagent:{info.Id.Name}" },
                    info.ContextData ?? [],
                    ct);
                var childContext = await ContextStore.OpenAsync(handle.SessionPath.Current, ct);
                foreach (var turn in info.Turns ?? [])
                {
                    await childContext.AppendAsync(turn, ct);
                }
                if (info.AutoStart)
                {
                    await EventBus.PublishAsync(
                        Event.WellKnown.Command.AgentStart with { Id = handle.SessionPath.Current },
                        new AgentStartRequest(handle),
                        ct);
                }
                return handle;
            }

            Service = new AgentHeartbeatService(
                scope,
                NullLogger<AgentHeartbeatService>.Instance,
                TransientAgentFactory,
                ContextStore,
                TimeProvider,
                EventBus,
                FileParserCache,
                Paths,
                PromptedAgentSpawner);
        }

        public IPromptedAgentSpawner PromptedAgentSpawner { get; }
    }
}
