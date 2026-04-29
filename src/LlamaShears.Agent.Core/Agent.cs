using System.Text;
using LlamaShears.Agent.Abstractions;
using LlamaShears.Provider.Abstractions;
using MessagePipe;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Agent.Core;

public sealed class Agent : IAgent
{
    private readonly ILanguageModel _model;
    private readonly ILogger<Agent> _logger;
    private readonly List<ModelTurn> _context;
    private readonly IDisposable _subscription;
    private int _heartbeating;

    public Agent(
        string id,
        TimeSpan heartbeatPeriod,
        ILanguageModel model,
        IAsyncSubscriber<SystemTick> ticks,
        IEnumerable<ModelTurn> seedContext,
        IReadOnlyList<IInputChannel> inputChannels,
        IReadOnlyList<IOutputChannel> outputChannels,
        ILogger<Agent> logger)
    {
        Id = id;
        HeartbeatPeriod = heartbeatPeriod;
        _model = model;
        _logger = logger;
        _context = [..seedContext];
        InputChannels = inputChannels;
        OutputChannels = outputChannels;
        _subscription = ticks.Subscribe(OnTickAsync);
    }

    public string Id { get; }

    public DateTimeOffset LastHeartbeatAt { get; private set; }

    public TimeSpan HeartbeatPeriod { get; }

    public bool HeartbeatEnabled { get; set; } = true;

    public IReadOnlyList<ModelTurn> Context => _context;

    public IReadOnlyList<IInputChannel> InputChannels { get; }

    public IReadOnlyList<IOutputChannel> OutputChannels { get; }

    public void Dispose()
    {
        _subscription.Dispose();
    }

    private async ValueTask OnTickAsync(SystemTick tick, CancellationToken cancellationToken)
    {
        if (!HeartbeatEnabled)
        {
            return;
        }

        if (LastHeartbeatAt != default && tick.At - LastHeartbeatAt < HeartbeatPeriod)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _heartbeating, 1, 0) != 0)
        {
            return;
        }

        try
        {
            await HeartbeatAsync(tick.At, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Exchange(ref _heartbeating, 0);
        }
    }

    private async Task HeartbeatAsync(DateTimeOffset firedAt, CancellationToken cancellationToken)
    {
        var contextSizeBefore = _context.Count;

        foreach (var input in InputChannels)
        {
            await foreach (var turn in input.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                _context.Add(turn);
            }
        }

        LastHeartbeatAt = firedAt;

        if (_context.Count == contextSizeBefore)
        {
            return;
        }

        var prompt = new ModelPrompt([.._context]);
        var content = new StringBuilder();

        await foreach (var fragment in _model.PromptAsync(prompt, cancellationToken).ConfigureAwait(false))
        {
            if (fragment is IModelTextResponse text)
            {
                content.Append(text.Content);
            }
        }

        if (content.Length == 0)
        {
            _logger.LogWarning("Agent '{AgentId}' received an empty response from the model.", Id);
            return;
        }

        var response = new ModelTurn(ModelRole.Assistant, content.ToString(), DateTimeOffset.UtcNow);
        _context.Add(response);

        foreach (var output in OutputChannels)
        {
            await output.SendAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }
}
