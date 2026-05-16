using System.Net;
using System.Net.Http.Headers;
using LlamaShears.Core.Common;
using LlamaShears.Core.Tools.ModelContextProtocol;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Tools.ModelContextProtocol;

public sealed class ModelContextProtocolRoutingHandlerTests
{
    [Test]
    public async Task RewritesSentinelHostToRegisteredEndpoint()
    {
        using var fixture = new Fixture();
        fixture.Registry.TryGet("github").Returns(new ModelContextProtocolServerOptions
        {
            Uri = new Uri("https://gh.example.com/mcp"),
        });

        var response = await fixture.SendAsync("http://github/");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(fixture.InnerHandler.LastRequest!.RequestUri!.ToString())
            .IsEqualTo("https://gh.example.com/mcp");
    }

    [Test]
    public async Task AppendsCallerPathOntoRegisteredEndpointPath()
    {
        using var fixture = new Fixture();
        fixture.Registry.TryGet("github").Returns(new ModelContextProtocolServerOptions
        {
            Uri = new Uri("https://gh.example.com/mcp"),
        });

        await fixture.SendAsync("http://github/messages");

        await Assert.That(fixture.InnerHandler.LastRequest!.RequestUri!.ToString())
            .IsEqualTo("https://gh.example.com/mcp/messages");
    }

    [Test]
    public async Task StampsConfiguredHeadersOverwritingCallerHeaders()
    {
        using var fixture = new Fixture();
        fixture.Registry.TryGet("github").Returns(new ModelContextProtocolServerOptions
        {
            Uri = new Uri("https://gh.example.com/mcp"),
            Headers = { ["Authorization"] = "Bearer config-token", ["X-Trace"] = "yes" },
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "http://github/");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "caller-token");

        using var client = new HttpClient(fixture.Handler) { BaseAddress = null };
        await client.SendAsync(request);

        var captured = fixture.InnerHandler.LastRequest!;
        await Assert.That(captured.Headers.Authorization!.ToString()).IsEqualTo("Bearer config-token");
        await Assert.That(captured.Headers.GetValues("X-Trace")).IsEquivalentTo(["yes"]);
    }

    [Test]
    public async Task Returns404JsonForUnknownServerWithoutCallingInner()
    {
        using var fixture = new Fixture();
        fixture.Registry.TryGet("github").Returns((ModelContextProtocolServerOptions?)null);

        var response = await fixture.SendAsync("http://github/");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(fixture.InnerHandler.LastRequest).IsNull();
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("unknown MCP server");
        await Assert.That(body).Contains("github");
        await Assert.That(response.Content.Headers.ContentType!.MediaType).IsEqualTo("application/json");
    }

    private sealed class Fixture : IDisposable
    {
        public Fixture()
        {
            Registry = Substitute.For<IModelContextProtocolServerRegistry>();
            UriMerger = new UriMerger();
            InnerHandler = new CapturingHandler();
            Handler = new ModelContextProtocolRoutingHandler(
                Registry,
                UriMerger,
                NullLogger<ModelContextProtocolRoutingHandler>.Instance)
            {
                InnerHandler = InnerHandler,
            };
        }

        public IModelContextProtocolServerRegistry Registry { get; }
        public IUriMerger UriMerger { get; }
        public CapturingHandler InnerHandler { get; }
        public ModelContextProtocolRoutingHandler Handler { get; }

        public async Task<HttpResponseMessage> SendAsync(string uri)
        {
            using var client = new HttpClient(Handler);
            return await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, uri));
        }

        public void Dispose() => Handler.Dispose();
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request });
        }
    }
}
