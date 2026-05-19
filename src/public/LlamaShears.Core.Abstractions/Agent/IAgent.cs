namespace LlamaShears.Core.Abstractions.Agent;

/// <summary>
/// An autonomous component that ingests input turns, drives a model,
/// and produces output turns. Identity, heartbeat cadence, channels,
/// and conversation state are internal and reachable through the
/// services that own the agent (config provider, context store,
/// message bus).
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Run the main agent loop
    /// </summary>
    Task RunAsync();
}
