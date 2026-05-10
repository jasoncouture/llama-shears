namespace LlamaShears.Core.Abstractions.Events;

/// <summary>
/// Constants for the framework's well-known event sources and event
/// types. Anchors the strings used in <see cref="EventType.Component"/>
/// and <see cref="EventType.EventName"/> in one place so subscribers
/// reference symbols instead of magic strings.
/// </summary>
public static class Event
{
    /// <summary>Canonical <see cref="EventType.Component"/> values.</summary>
    public static class Sources
    {
        /// <summary>Events emitted by an agent.</summary>
        public const string Agent = "agent";

        /// <summary>Events emitted by the host process itself.</summary>
        public const string Host = "system";
        /// <summary>Events emitted by chat channels.</summary>
        public const string Channel = "channel";
    }

    /// <summary>Pre-built <see cref="EventType"/> instances for each well-known event.</summary>
    public static class WellKnown
    {
        /// <summary>Host-level events.</summary>
        public static class Host
        {
            /// <summary>Host has finished starting.</summary>
            public static EventType Startup { get; } = new EventType(Sources.Host, "startup");
            /// <summary>Host is beginning shutdown.</summary>
            public static EventType Shutdown { get; } = new EventType(Sources.Host, "shutdown");
            /// <summary>Periodic wall-clock heartbeat tick.</summary>
            public static EventType Tick { get; } = new EventType(Sources.Host, "tick");
        }
        /// <summary>Agent-level events.</summary>
        public static class Agent
        {
            /// <summary>An agent has been loaded into the host.</summary>
            public static EventType Loaded { get; } = new EventType(Sources.Agent, "loaded");
            /// <summary>An agent has been unloaded from the host.</summary>
            public static EventType Unloaded { get; } = new EventType(Sources.Agent, "unloaded");
            /// <summary>Loading an agent failed.</summary>
            public static EventType LoadError { get; } = new EventType(Sources.Agent, "loading-error");
            /// <summary>Agent has begun processing a turn.</summary>
            public static EventType Busy { get; } = new EventType(Sources.Agent, "busy");
            /// <summary>Agent has finished processing and is idle again.</summary>
            public static EventType Idle { get; } = new EventType(Sources.Agent, "idle");
            /// <summary>Streaming user-visible message fragment.</summary>
            public static EventType Message { get; } = new EventType(Sources.Agent, "message");
            /// <summary>Streaming hidden-thought fragment.</summary>
            public static EventType Thought { get; } = new EventType(Sources.Agent, "thought");
            /// <summary>The agent is dispatching a tool call.</summary>
            public static EventType ToolCall { get; } = new EventType(Sources.Agent, "tool-call");
            /// <summary>A tool call has produced a result.</summary>
            public static EventType ToolResult { get; } = new EventType(Sources.Agent, "tool-result");
            /// <summary>Context compaction has started.</summary>
            public static EventType CompactingStarted { get; } = new EventType(Sources.Agent, "compacting-started");
            /// <summary>Context compaction has finished.</summary>
            public static EventType CompactingFinished { get; } = new EventType(Sources.Agent, "compacting-finished");
            /// <summary>A complete turn has been recorded to the agent's context log.</summary>
            public static EventType Turn { get; } = new EventType(Sources.Agent, "turn");
        }
        /// <summary>Channel-level events.</summary>
        public static class Channel
        {
            /// <summary>A channel has come into existence.</summary>
            public static EventType Created { get; } = new EventType(Sources.Channel, "created");
            /// <summary>A channel has been torn down.</summary>
            public static EventType Destroyed { get; } = new EventType(Sources.Channel, "destroyed");
            /// <summary>A user-authored message arrived on the channel.</summary>
            public static EventType Message { get; } = new EventType(Sources.Channel, "message");
            /// <summary>An error condition was reported on the channel.</summary>
            public static EventType Error { get; } = new EventType(Sources.Channel, "error");
        }
    }
}
