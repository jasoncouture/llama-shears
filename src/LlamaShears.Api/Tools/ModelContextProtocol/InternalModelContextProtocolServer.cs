using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace LlamaShears.Api.Tools.ModelContextProtocol;

public sealed class InternalModelContextProtocolServer : IInternalModelContextProtocolServer
{
    internal const string McpPath = "/mcp";

    private readonly IServer _server;

    public InternalModelContextProtocolServer(IServer server)
    {
        _server = server;
    }

    public Uri? Uri
    {
        get
        {
            var first = _server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();
            return first is null ? null : new Uri(new Uri(first), McpPath);
        }
    }
}
