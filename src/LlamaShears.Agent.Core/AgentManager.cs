using System.Diagnostics;
using System.Text.Json.Nodes;
using LlamaShears.Agent.Abstractions;
using LlamaShears.Hosting;
using LlamaShears.Hosting.Abstractions;
using MessagePipe;

namespace LlamaShears.Agent.Core;

public sealed class AgentManager : IHostStartupTask, IDisposable
{
    private readonly IAsyncSubscriber<SystemTick> _ticks;
    private readonly Dictionary<string, AgentSlot> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private IDisposable? _subscription;
    private int _reconciling;

    public AgentManager(IAsyncSubscriber<SystemTick> ticks)
    {
        _ticks = ticks;
    }

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

        var directory = new DirectoryInfo(LlamaShearsPaths.AgentsRoot);
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
        Debug.WriteLine($"AgentManager: started agent '{name}' from {file.Path}");
    }

    private void Reload(string name, FilePresence file)
    {
        var slot = TryLoad(name, file);
        if (slot is null)
        {
            // Keep the previous slot intact: a half-saved or syntactically
            // broken file shouldn't blow away a working agent.
            return;
        }

        _loaded[name] = slot;
        Debug.WriteLine($"AgentManager: reloaded agent '{name}' from {file.Path}");
    }

    private void Stop(string name)
    {
        if (_loaded.Remove(name, out var slot))
        {
            Debug.WriteLine($"AgentManager: stopped agent '{slot.Name}'");
        }
    }

    private static AgentSlot? TryLoad(string name, FilePresence file)
    {
        JsonNode? config;
        try
        {
            using var stream = File.OpenRead(file.Path);
            config = JsonNode.Parse(stream);
        }
        catch (Exception ex) when (ex is IOException or System.Text.Json.JsonException)
        {
            Debug.WriteLine($"AgentManager: skipping agent '{name}' from {file.Path}: {ex.Message}");
            return null;
        }

        if (config is null)
        {
            Debug.WriteLine($"AgentManager: skipping agent '{name}' from {file.Path}: empty document");
            return null;
        }

        return new AgentSlot(name, file.Path, config, file.Fingerprint);
    }

    // Treat Config as immutable: parsed once on load and never mutated,
    // so a snapshot handed to an in-flight interaction stays valid even
    // when Reload swaps the slot for a newer version.
    private sealed record AgentSlot(string Name, string Path, JsonNode Config, FileFingerprint Fingerprint);

    private readonly record struct FilePresence(string Path, FileFingerprint Fingerprint);

    private readonly record struct FileFingerprint(DateTimeOffset LastWriteTimeUtc, long Length);
}
