namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// An autonomous component that ingests input turns, drives a model,
/// and produces output turns. Identified by <see cref="Id"/>; the
/// rest of its surface — heartbeat cadence, channels, conversation
/// state — is internal and reachable through the services that own
/// it (config provider, context store, message bus).
/// </summary>
public interface IAgent : IDisposable
{
    /// <summary>Stable identifier for this agent.</summary>
    string Id { get; }
}
