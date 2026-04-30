using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using LlamaShears.Core.Abstractions.Agent;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core;

public sealed class InMemoryAgentTokenStore : IAgentTokenStore
{
    private const int TokenByteLength = 32;

    private readonly ConcurrentDictionary<string, Entry> _entries = new(StringComparer.Ordinal);
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<AgentTokenStoreOptions> _options;

    public InMemoryAgentTokenStore(
        TimeProvider timeProvider,
        IOptions<AgentTokenStoreOptions> options)
    {
        _timeProvider = timeProvider;
        _options = options;
    }

    public string Issue(AgentInfo agent)
    {
        ArgumentNullException.ThrowIfNull(agent);

        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        var token = Convert.ToBase64String(bytes);
        var expiry = _timeProvider.GetUtcNow() + _options.Value.TokenLifetime;
        _entries[token] = new Entry(agent, expiry);
        return token;
    }

    public bool TryGetAgentInformation(string token, [NotNullWhen(true)] out AgentInfo? agent)
    {
        ArgumentNullException.ThrowIfNull(token);

        if (!_entries.TryRemove(token, out var entry))
        {
            agent = null;
            return false;
        }

        if (_timeProvider.GetUtcNow() >= entry.Expiry)
        {
            agent = null;
            return false;
        }

        agent = entry.Agent;
        return true;
    }

    internal int Sweep()
    {
        var now = _timeProvider.GetUtcNow();
        var removed = 0;
        foreach (var kv in _entries)
        {
            if (now >= kv.Value.Expiry && _entries.TryRemove(kv))
            {
                removed++;
            }
        }
        return removed;
    }

    private readonly record struct Entry(AgentInfo Agent, DateTimeOffset Expiry);
}
