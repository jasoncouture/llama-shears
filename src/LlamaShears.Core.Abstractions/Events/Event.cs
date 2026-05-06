namespace LlamaShears.Core.Abstractions.Events;

public static class Event
{
    public static class Sources
    {
        public const string Agent = "agent";

        public const string System = "system";
    }

    public static class WellKnown
    {
        public static EventType HostStartup { get; } = new(Sources.System, "startup");

        public static EventType HostShutdown { get; } = new(Sources.System, "shutdown");

        public static EventType AgentLoaded { get; } = new(Sources.Agent, "loaded");

        public static EventType AgentUnloaded { get; } = new(Sources.Agent, "unloaded");

        public static EventType AgentLoadError { get; } = new(Sources.Agent, "loading-error");

        public static EventType AgentBusy { get; } = new(Sources.Agent, "busy");

        public static EventType AgentIdle { get; } = new(Sources.Agent, "idle");
    }
}
