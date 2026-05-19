using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Core.Abstractions.Events.Agent;

/// <summary>
/// Payload for <see cref="Event.WellKnown.Command.AgentLoad"/>. Carries
/// the resolved <see cref="AgentConfig"/> the manager should bring up
/// (or replace an existing slot with). <see cref="EventType.Id"/> on the
/// envelope holds the target agent id.
/// </summary>
/// <param name="Config">Immutable agent configuration snapshot to load.</param>
public sealed record AgentLoadRequest(AgentConfig Config);
