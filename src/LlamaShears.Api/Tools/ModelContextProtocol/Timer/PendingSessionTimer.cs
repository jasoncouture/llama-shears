using LlamaShears.Core.Abstractions.Agent.Sessions;

namespace LlamaShears.Api.Tools.ModelContextProtocol.Timer;

internal sealed class PendingSessionTimer
{
    public PendingSessionTimer(SessionId session, string prompt, DateTimeOffset firesAt)
    {
        Session = session;
        Prompt = prompt;
        FiresAt = firesAt;
    }

    public SessionId Session { get; }

    public string Prompt { get; }

    public DateTimeOffset FiresAt { get; }

    public ITimer? Timer { get; set; }
}
