using System.Net;
using System.Net.Http.Headers;
using LlamaShears.Core.Abstractions.Agent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class LoopbackBearerHandler : DelegatingHandler
{
    private readonly IInternalModelContextProtocolServer _internalServer;
    private readonly IAgentTokenStore _tokenStore;
    private readonly ICurrentAgentAccessor _currentAgent;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<LoopbackBearerHandler> _logger;

    public LoopbackBearerHandler(
        IInternalModelContextProtocolServer internalServer,
        IAgentTokenStore tokenStore,
        ICurrentAgentAccessor currentAgent,
        IHostApplicationLifetime appLifetime,
        ILogger<LoopbackBearerHandler> logger)
    {
        _internalServer = internalServer;
        _tokenStore = tokenStore;
        _currentAgent = currentAgent;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsLoopback(request.RequestUri))
        {
            // McpClient.DisposeAsync fires a DELETE during shutdown to tear
            // down its session. Acknowledge it locally — the listener is on
            // its way out, and pushing the request through would 401 (we
            // have no agent scope at that point) and spam an "MCP shutdown
            // failed" log line.
            if (_appLifetime.ApplicationStopping.IsCancellationRequested
                && request.Method == HttpMethod.Delete)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                });
            }
            var agent = _currentAgent.Current
                ?? throw new InvalidOperationException(
                    "Outbound MCP request targets the internal listener but no agent is on the current call's ICurrentAgentAccessor scope; the caller must establish a scope before issuing tool calls.");
            var token = _tokenStore.Issue(agent);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            LogInjectedBearer(_logger, agent.AgentId, request.RequestUri!);
        }
        return base.SendAsync(request, cancellationToken);
    }

    private bool IsLoopback(Uri? requestUri)
    {
        if (requestUri is null)
        {
            return false;
        }
        var internalUri = _internalServer.Uri;
        if (internalUri is null)
        {
            return false;
        }
        if (requestUri.Port != internalUri.Port)
        {
            return false;
        }
        if (string.Equals(requestUri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return IPAddress.TryParse(requestUri.Host, out var address) && IPAddress.IsLoopback(address);
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Injected loopback bearer for agent '{AgentId}' on request to {RequestUri}.")]
    private static partial void LogInjectedBearer(ILogger logger, string agentId, Uri requestUri);
}
