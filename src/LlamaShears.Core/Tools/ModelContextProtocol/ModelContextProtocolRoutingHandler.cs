using System.Net;
using System.Net.Http.Headers;
using System.Text;
using LlamaShears.Core.Common;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class ModelContextProtocolRoutingHandler : DelegatingHandler
{
    private readonly IModelContextProtocolServerRegistry _registry;
    private readonly IUriMerger _uriMerger;
    private readonly ILogger<ModelContextProtocolRoutingHandler> _logger;

    public ModelContextProtocolRoutingHandler(
        IModelContextProtocolServerRegistry registry,
        IUriMerger uriMerger,
        ILogger<ModelContextProtocolRoutingHandler> logger)
    {
        _registry = registry;
        _uriMerger = uriMerger;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sentinelUri = request.RequestUri;
        if (sentinelUri is null)
        {
            return Task.FromResult(BuildNotFoundResponse(request, serverName: "<missing>"));
        }

        var serverName = sentinelUri.Host;
        var config = _registry.TryGet(serverName);
        if (config is null)
        {
            LogUnknownServer(serverName);
            return Task.FromResult(BuildNotFoundResponse(request, serverName));
        }

        request.RequestUri = _uriMerger.Merge(config.Uri, sentinelUri);
        ApplyHeaders(request, config.Headers);
        return base.SendAsync(request, cancellationToken);
    }

    private static void ApplyHeaders(HttpRequestMessage request, IReadOnlyDictionary<string, string> headers)
    {
        if (headers.Count == 0)
        {
            return;
        }
        foreach (var (name, value) in headers)
        {
            request.Headers.Remove(name);
            request.Headers.TryAddWithoutValidation(name, value);
        }
    }

    private static HttpResponseMessage BuildNotFoundResponse(HttpRequestMessage request, string serverName)
    {
        var payload = $"{{\"error\":\"unknown MCP server '{serverName}'\"}}";
        return new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            RequestMessage = request,
            Content = new StringContent(payload, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json")),
        };
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "MCP routing rejected request for unknown server '{ServerName}'.")]
    private partial void LogUnknownServer(string serverName);
}
