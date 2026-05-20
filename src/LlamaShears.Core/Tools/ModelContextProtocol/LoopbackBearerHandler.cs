using System.Net;
using System.Net.Http.Headers;
using LlamaShears.Core.Abstractions.Agent;
using LlamaShears.Core.Abstractions.Agent.Sessions;
using LlamaShears.Core.Abstractions.Common;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LlamaShears.Core.Tools.ModelContextProtocol;

public sealed partial class LoopbackBearerHandler : DelegatingHandler
{
    private readonly IInternalModelContextProtocolServer _internalServer;
    private readonly IAgentTokenStore _tokenStore;
    private readonly IDataContextFactory _dataContextFactory;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<LoopbackBearerHandler> _logger;

    public LoopbackBearerHandler(
        IInternalModelContextProtocolServer internalServer,
        IAgentTokenStore tokenStore,
        IDataContextFactory dataContextFactory,
        IHostApplicationLifetime appLifetime,
        ILogger<LoopbackBearerHandler> logger)
    {
        _internalServer = internalServer;
        _tokenStore = tokenStore;
        _dataContextFactory = dataContextFactory;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (IsLoopback(request.RequestUri))
        {
            if (_appLifetime.ApplicationStopping.IsCancellationRequested
                && request.Method == HttpMethod.Delete)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    RequestMessage = request,
                });
            }
            var scope = _dataContextFactory.Current
                ?? throw new InvalidOperationException(
                    "Outbound MCP request targets the internal listener but no agent data scope is ambient on the current call; the caller must enter an agent scope before issuing tool calls.");
            var session = scope.GetCurrentSessionId();
            var model = scope.GetModelConfiguration();
            var agent = new AgentInfo(
                Session: session,
                ModelId: model.Id,
                ContextWindowSize: model.ContextLength ?? 0);
            var token = _tokenStore.Issue(agent);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            LogInjectedBearer(session, request.RequestUri!);
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

    [LoggerMessage(Level = LogLevel.Trace, Message = "Injected loopback bearer for session '{Session}' on request to {RequestUri}.")]
    private partial void LogInjectedBearer(SessionId session, Uri requestUri);
}
