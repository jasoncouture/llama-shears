using System.Collections.Immutable;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolToolDiscovery : IModelContextProtocolToolDiscovery
{
    private readonly IModelContextProtocolClient _client;
    private readonly ILogger<ModelContextProtocolToolDiscovery> _logger;

    public ModelContextProtocolToolDiscovery(
        IModelContextProtocolClient client,
        ILogger<ModelContextProtocolToolDiscovery> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async ValueTask<ImmutableArray<ToolGroup>> DiscoverAsync(
        IEnumerable<string> serverNames,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(serverNames);

        var builder = ImmutableArray.CreateBuilder<ToolGroup>();
        foreach (var name in serverNames)
        {
            var group = await DiscoverServerAsync(name, cancellationToken);
            if (group is not null)
            {
                builder.Add(group);
            }
        }
        return builder.ToImmutable();
    }

    private async Task<ToolGroup?> DiscoverServerAsync(
        string serverName,
        CancellationToken cancellationToken)
    {
        try
        {
            var tools = await _client.ListToolsAsync(serverName, cancellationToken);
            return new ToolGroup(Source: serverName, Tools: tools);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogServerDiscoveryFailed(serverName, ex.Message, ex);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "MCP discovery failed for server '{ServerName}': {Message}")]
    private partial void LogServerDiscoveryFailed(string serverName, string message, Exception ex);
}
