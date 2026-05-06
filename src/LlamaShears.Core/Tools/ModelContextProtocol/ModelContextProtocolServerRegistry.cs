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

    public IReadOnlyDictionary<string, Uri> Resolve(ImmutableHashSet<string>? whitelist)
    {
        var all = BuildAllKnown();

        if (whitelist is null)
        {
            return all;
        }

        var resolved = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in whitelist)
        {
            if (all.TryGetValue(name, out var uri))
            {
                resolved[name] = uri;
            }
            else
            {
                LogUnknownServer(_logger, name);
            }
        }
        return resolved;
    }

    // The "all known" view includes the host's own internal MCP
    // endpoint under a fixed name. It is published into the registry
    // like any user-configured server, so an agent that wants it has
    // to either omit a whitelist (default = all known) or name it
    // explicitly.
    private Dictionary<string, Uri> BuildAllKnown()
    {
        var configured = _options.CurrentValue.Servers;
        var all = new Dictionary<string, Uri>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, uri) in configured)
        {
            all[name] = uri;
        }
        if (_internal.Uri is { } internalUri)
        {
            all[InternalServerName] = internalUri;
        }
        return all;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "MCP server '{Name}' is whitelisted by an agent but is not registered; skipping.")]
    private static partial void LogUnknownServer(ILogger logger, string name);
}
