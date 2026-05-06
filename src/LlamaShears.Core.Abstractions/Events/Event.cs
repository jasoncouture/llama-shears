namespace LlamaShears.Core.Abstractions.Events;

public static class Event
{
    public static class Sources
    {
        public const string Agent = "agent";

        public const string Host = "system";
        public const string Channel = "channel";
    }

    public static class WellKnown
    {
        public static class Host
        {
            public static EventType Startup { get; } = new(Sources.Host, "startup");
            public static EventType Shutdown { get; } = new(Sources.Host, "shutdown");
        }
        public static class Agent
        {

            public static EventType Loaded { get; } = new(Sources.Agent, "loaded");
            public static EventType Unloaded { get; } = new(Sources.Agent, "unloaded");
            public static EventType LoadError { get; } = new(Sources.Agent, "loading-error");
            public static EventType Busy { get; } = new(Sources.Agent, "busy");
            public static EventType Idle { get; } = new(Sources.Agent, "idle");
            public static EventType Message { get; } = new(Sources.Agent, "message");
            public static EventType Thought { get; } = new(Sources.Agent, "thought");
        }
        public static class Channel
        {
            public static EventType Created { get; } = new(Sources.Channel, "created");
            public static EventType Destroyed { get; } = new(Sources.Channel, "destroyed");
            public static EventType Message { get; } = new(Sources.Channel, "message");
            public static EventType Error { get; } = new(Sources.Channel, "error");
        }
    }
}
