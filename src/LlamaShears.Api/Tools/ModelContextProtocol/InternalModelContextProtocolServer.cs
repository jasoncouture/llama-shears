using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace LlamaShears.Api.Tools.ModelContextProtocol;

public sealed class InternalModelContextProtocolServer : IInternalModelContextProtocolServer
{
    internal const string McpPath = "/mcp";
    private const string LoopbackHost = "localhost";

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
            if (first is null)
            {
                return null;
            }
            // The bound address is whatever Kestrel ended up listening
            // on — frequently a wildcard like `http://[::]:8080` (the
            // resolved form of `+`). Wildcards are valid bind targets
            // but invalid dial targets (HttpClient throws on `[::]` /
            // `0.0.0.0`). Self-discovery only ever needs to reach the
            // loopback, so we keep the port and scheme but force the
            // host to localhost.
            var listen = new Uri(first);
            var builder = new UriBuilder(listen)
            {
                Host = LoopbackHost,
                Path = McpPath,
            };
            return builder.Uri;
        }
    }
}
