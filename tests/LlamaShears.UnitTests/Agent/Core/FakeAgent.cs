using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class FakeAgent : IAgent
{
    public DateTimeOffset LastHeartbeatAt { get; init; }

    public TimeSpan HeartbeatPeriod { get; init; } = TimeSpan.FromMinutes(30);

    public bool HeartbeatEnabled { get; init; } = true;

    public IReadOnlyList<ModelTurn> Context { get; init; } = [];

    public IReadOnlyList<IInputChannel> InputChannels { get; init; } = [];

    public IReadOnlyList<IOutputChannel> OutputChannels { get; init; } = [];

    public void Dispose()
    {
    }
}
