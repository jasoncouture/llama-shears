using System.Diagnostics;
using System.Text.Json;
using LlamaShears.Agent.Abstractions;
using LlamaShears.Agent.Abstractions.Events;
using LlamaShears.Agent.Core.Channels;
using LlamaShears.Agent.Core.SystemPrompt;
using LlamaShears.Hosting;
using LlamaShears.Provider.Abstractions;
using MessagePipe;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Agent.Core;

public sealed class AgentManager : IHostStartupTask, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAsyncSubscriber<SystemTick> _ticks;
    private readonly IEnumerable<IProviderFactory> _providers;
    private readonly IShearsPaths _paths;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<AgentManager> _logger;
    private readonly ISystemPromptProvider _systemPromptProvider;
    private readonly TimeProvider _timeProvider;
    private readonly IAsyncSubscriber<UserMessageSubmitted> _userMessages;
    private readonly IAsyncPublisher<AgentTurnEmitted> _turnPublisher;
    private readonly IAsyncPublisher<AgentFragmentEmitted> _fragmentPublisher;
    private readonly Dictionary<string, AgentSlot> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private IDisposable? _subscription;
    private int _reconciling;

    public AgentManager(
        IAsyncSubscriber<SystemTick> ticks,
        IEnumerable<IProviderFactory> providers,
        IShearsPaths paths,
        ILoggerFactory loggerFactory,
        ISystemPromptProvider systemPromptProvider,
        TimeProvider timeProvider,
        IAsyncSubscriber<UserMessageSubmitted> userMessages,
        IAsyncPublisher<AgentTurnEmitted> turnPublisher,
        IAsyncPublisher<AgentFragmentEmitted> fragmentPublisher)
    {
        _ticks = ticks;
        _providers = providers;
        _paths = paths;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AgentManager>();
        _systemPromptProvider = systemPromptProvider;
        _timeProvider = timeProvider;
        _userMessages = userMessages;
        _turnPublisher = turnPublisher;
        _fragmentPublisher = fragmentPublisher;
    }

    public IReadOnlyDictionary<string, IAgent> Agents
        => _loaded.ToDictionary(kv => kv.Key, kv => kv.Value.Agent, StringComparer.OrdinalIgnoreCase);

    public ValueTask StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _ticks.Subscribe(OnTickAsync);
        return ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _subscription = null;

        foreach (var name in _loaded.Keys.ToArray())
        {
            Stop(name);
        }
    }

    private ValueTask OnTickAsync(SystemTick tick, CancellationToken cancellationToken)
    {
        // Skip if a previous tick is still reconciling. Disk I/O on a
        // 30s tick should never overlap, but a slow filesystem could
        // queue up handlers; collapsing the backlog is correct.
        if (Interlocked.CompareExchange(ref _reconciling, 1, 0) != 0)
        {
            return ValueTask.CompletedTask;
        }

        try
        {
            Reconcile();
        }
        finally
        {
            Interlocked.Exchange(ref _reconciling, 0);
        }

        return ValueTask.CompletedTask;
    }

    private void Reconcile()
    {
        var present = new Dictionary<string, FilePresence>(StringComparer.OrdinalIgnoreCase);

        var directory = new DirectoryInfo(_paths.GetPath(PathKind.Agents));
        foreach (var file in directory.EnumerateFiles("*.json", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileNameWithoutExtension(file.Name);
            present[name] = new FilePresence(
                file.FullName,
                new FileFingerprint(new DateTimeOffset(file.LastWriteTimeUtc), file.Length));
        }

        foreach (var (name, file) in present)
        {
            if (!_loaded.TryGetValue(name, out var slot))
            {
                Start(name, file);
            }
            else if (slot.Fingerprint != file.Fingerprint)
            {
                Reload(name, file);
            }
        }

        foreach (var name in _loaded.Keys.Where(k => !present.ContainsKey(k)).ToArray())
        {
            Stop(name);
        }
    }

    private void Start(string name, FilePresence file)
    {
        var slot = TryLoad(name, file);
        if (slot is null)
        {
            return;
        }

        _loaded[name] = slot;
        _logger.LogInformation("Started agent '{AgentId}' from {Path}", name, file.Path);
    }

    private void Reload(string name, FilePresence file)
    {
        var newSlot = TryLoad(name, file);
        if (newSlot is null)
        {
            // Keep the previous slot intact: a half-saved or syntactically
            // broken file shouldn't blow away a working agent.
            return;
        }

        if (_loaded.Remove(name, out var oldSlot))
        {
            oldSlot.Agent.Dispose();
        }

        _loaded[name] = newSlot;
        _logger.LogInformation("Reloaded agent '{AgentId}' from {Path}", name, file.Path);
    }

    private void Stop(string name)
    {
        if (_loaded.Remove(name, out var slot))
        {
            slot.Agent.Dispose();
            _logger.LogInformation("Stopped agent '{AgentId}'", slot.Name);
        }
    }

    private AgentSlot? TryLoad(string name, FilePresence file)
    {
        AgentConfig? config;
        try
        {
            using var stream = File.OpenRead(file.Path);
            config = JsonSerializer.Deserialize<AgentConfig>(stream, _jsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _logger.LogWarning(ex, "Skipping agent '{AgentId}' from {Path}: {Message}", name, file.Path, ex.Message);
            return null;
        }

        if (config is null)
        {
            _logger.LogWarning("Skipping agent '{AgentId}' from {Path}: empty document", name, file.Path);
            return null;
        }

        IAgent agent;
        try
        {
            agent = BuildAgent(name, config);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            _logger.LogWarning(ex, "Skipping agent '{AgentId}' from {Path}: {Message}", name, file.Path, ex.Message);
            return null;
        }

        return new AgentSlot(name, file.Path, agent, file.Fingerprint);
    }

    private IAgent BuildAgent(string name, AgentConfig config)
    {
        var providerFactory = _providers.FirstOrDefault(p =>
            string.Equals(p.Name, config.Model.Id.Provider, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"No provider factory registered with name '{config.Model.Id.Provider}'.");

        var model = providerFactory.CreateModel(new ModelConfiguration(
            ModelId: config.Model.Id.Model,
            Think: config.Model.Think,
            ContextLength: config.Model.ContextLength,
            KeepAlive: config.Model.KeepAlive));

        var now = _timeProvider.GetUtcNow();
        var systemPrompt = _systemPromptProvider.Build(name, now);
        var seedContext = new List<ModelTurn>
        {
            new(ModelRole.System, systemPrompt, now),
        };

        var inputs = new List<IInputChannel>
        {
            new UiInputChannel(name, _userMessages),
        };

        var outputs = new List<IOutputChannel>
        {
            new UiOutputChannel(name, _turnPublisher),
            new LoggerOutputChannel(_loggerFactory.CreateLogger<LoggerOutputChannel>(), name),
        };

        return new Agent(
            id: name,
            heartbeatPeriod: config.HeartbeatPeriod,
            model: model,
            ticks: _ticks,
            seedContext: seedContext,
            inputChannels: inputs,
            outputChannels: outputs,
            logger: _loggerFactory.CreateLogger<Agent>(),
            fragments: _fragmentPublisher);
    }

    private sealed record AgentSlot(string Name, string Path, IAgent Agent, FileFingerprint Fingerprint);

    private readonly record struct FilePresence(string Path, FileFingerprint Fingerprint);

    private readonly record struct FileFingerprint(DateTimeOffset LastWriteTimeUtc, long Length);
}
