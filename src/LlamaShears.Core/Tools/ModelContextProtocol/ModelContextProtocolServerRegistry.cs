using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolServerRegistry : IModelContextProtocolServerRegistry
{
    private const string InternalServerName = "llamashears";

    private readonly IOptionsMonitor<ModelContextProtocolOptions> _options;
    private readonly IInternalModelContextProtocolServer _internal;
    private readonly ILogger<ModelContextProtocolServerRegistry> _logger;

    public ModelContextProtocolServerRegistry(
        IOptionsMonitor<ModelContextProtocolOptions> options,
        IInternalModelContextProtocolServer @internal,
        ILogger<ModelContextProtocolServerRegistry> logger)
    {
        _options = options;
        _internal = @internal;
        _logger = logger;
    }

    public IReadOnlyDictionary<string, ModelContextProtocolServerOptions> Resolve(ImmutableHashSet<string>? whitelist)
    {
        var all = BuildAllKnown();

        if (whitelist is null)
        {
            return all;
        }

        var resolved = new Dictionary<string, ModelContextProtocolServerOptions>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in whitelist)
        {
            if (all.TryGetValue(name, out var config))
            {
                resolved[name] = config;
            }
            else
            {
                LogUnknownServer(name);
            }
        }
        return resolved;
    }

    public ModelContextProtocolServerOptions? TryGet(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var all = BuildAllKnown();
        return all.TryGetValue(name, out var config) ? config : null;
    }

    private Dictionary<string, ModelContextProtocolServerOptions> BuildAllKnown()
    {
        var configured = _options.CurrentValue.Servers;
        var all = new Dictionary<string, ModelContextProtocolServerOptions>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, config) in configured)
        {
            all[name] = config;
        }
        if (_internal.Uri is { } internalUri)
        {
            all[InternalServerName] = new ModelContextProtocolServerOptions { Uri = internalUri };
        }
        return all;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "MCP server '{Name}' is whitelisted by an agent but is not registered; skipping.")]
    private partial void LogUnknownServer(string name);
}
