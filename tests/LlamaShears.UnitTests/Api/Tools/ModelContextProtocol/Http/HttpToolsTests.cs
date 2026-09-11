using System.Net;
using System.Text;
using LlamaShears.Api.Tools.ModelContextProtocol.Filesystem;
using LlamaShears.Api.Tools.ModelContextProtocol.Http;
using LlamaShears.Core.Paths;
using LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Filesystem;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace LlamaShears.UnitTests.Api.Tools.ModelContextProtocol.Http;

public sealed class HttpToolsTests
{
    [Test]
    public async Task GetReturnsStatusHeadersAndBody()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, """{"ok":true}""", "application/json");
        var tool = CreateTool(handler);

        var result = await tool.Request("https://example.com/api", cancellationToken: CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Completed).IsTrue();
        await Assert.That(result.Ok).IsTrue();
        await Assert.That(result.Status).IsEqualTo(200);
        await Assert.That(result.Body).IsEqualTo("""{"ok":true}""");
        await Assert.That(result.ContentType).Contains("application/json");
        await Assert.That(result.Truncated).IsFalse();
        await Assert.That(result.Binary).IsFalse();
        await Assert.That(result.TimedOut).IsFalse();
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Get);
    }

    [Test]
    public async Task HttpErrorStatusCompletesWithoutToolError()
    {
        var tool = CreateTool(new RecordingHandler(HttpStatusCode.NotFound, """{"error":"missing"}""", "application/json"));

        var result = await tool.Request("https://example.com/missing", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsTrue();
        await Assert.That(result.Ok).IsFalse();
        await Assert.That(result.Status).IsEqualTo(404);
        await Assert.That(result.Body).Contains("missing");
        await Assert.That(result.Error).IsNull();
    }

    [Test]
    public async Task PostSendsJsonBodyAndDefaultsContentType()
    {
        var handler = new RecordingHandler(HttpStatusCode.Created, """{"id":1}""", "application/json");
        var tool = CreateTool(handler);

        var result = await tool.Request(
            "https://example.com/items",
            method: "POST",
            body: """{"name":"x"}""",
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsTrue();
        await Assert.That(result.Status).IsEqualTo(201);
        await Assert.That(handler.LastRequest!.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(handler.LastBody).IsEqualTo("""{"name":"x"}""");
        await Assert.That(handler.LastRequest.Content!.Headers.ContentType!.MediaType).IsEqualTo("application/json");
    }

    [Test]
    public async Task PostPlainBodyDefaultsToTextPlain()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "ok", "text/plain");
        var tool = CreateTool(handler);

        await tool.Request(
            "https://example.com/plain",
            method: "POST",
            body: "hello",
            cancellationToken: CancellationToken.None);

        await Assert.That(handler.LastRequest!.Content!.Headers.ContentType!.MediaType).IsEqualTo("text/plain");
    }

    [Test]
    public async Task CustomHeadersAreSent()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "{}", "application/json");
        var tool = CreateTool(handler);

        var result = await tool.Request(
            "https://example.com/secure",
            headers: new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer secret",
                ["Accept"] = "application/json",
            },
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsTrue();
        await Assert.That(handler.LastRequest!.Headers.Authorization!.Scheme).IsEqualTo("Bearer");
        await Assert.That(handler.LastRequest.Headers.Authorization.Parameter).IsEqualTo("secret");
        await Assert.That(handler.LastRequest.Headers.Accept.ToString()).Contains("application/json");
    }

    [Test]
    public async Task CallerContentTypeIsHonored()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "ok", "text/plain");
        var tool = CreateTool(handler);

        await tool.Request(
            "https://example.com/form",
            method: "POST",
            headers: new Dictionary<string, string> { ["Content-Type"] = "application/x-www-form-urlencoded" },
            body: "a=1",
            cancellationToken: CancellationToken.None);

        await Assert.That(handler.LastRequest!.Content!.Headers.ContentType!.MediaType)
            .IsEqualTo("application/x-www-form-urlencoded");
    }

    [Test]
    public async Task RefusesWithoutAnAuthenticatedAgent()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "x");
        var tool = CreateTool(handler, agentId: null);

        var result = await tool.Request("https://example.com/", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("authenticated agent");
        await Assert.That(handler.LastRequest).IsNull();
    }

    [Test]
    public async Task RefusesNonHttpSchemes()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "x");
        var tool = CreateTool(handler);

        var result = await tool.Request("file:///etc/passwd", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("http or https");
        await Assert.That(handler.LastRequest).IsNull();
    }

    [Test]
    public async Task RefusesMissingUrl()
    {
        var tool = CreateTool(new RecordingHandler(HttpStatusCode.OK, "x"));

        var result = await tool.Request("  ", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("url is required");
    }

    [Test]
    public async Task RefusesUnknownMethod()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "x");
        var tool = CreateTool(handler);

        var result = await tool.Request("https://example.com/", method: "CONNECT", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("CONNECT");
        await Assert.That(handler.LastRequest).IsNull();
    }

    [Test]
    public async Task RefusesBodyOnGet()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "x");
        var tool = CreateTool(handler);

        var result = await tool.Request(
            "https://example.com/",
            method: "GET",
            body: "{}",
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("GET cannot include a body");
        await Assert.That(handler.LastRequest).IsNull();
    }

    [Test]
    public async Task RefusesForbiddenHeaders()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK, "x");
        var tool = CreateTool(handler);

        var result = await tool.Request(
            "https://example.com/",
            headers: new Dictionary<string, string> { ["Host"] = "evil.test" },
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("Host");
        await Assert.That(handler.LastRequest).IsNull();
    }

    [Test]
    public async Task RefusesTimeoutOutsideRange()
    {
        var tool = CreateTool(new RecordingHandler(HttpStatusCode.OK, "x"));

        var result = await tool.Request(
            "https://example.com/",
            timeoutSeconds: 0,
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("timeoutSeconds");
    }

    [Test]
    public async Task TruncatesLargeTextBodies()
    {
        var payload = new string('a', HttpTools.MaxResponseBodyBytes + 32);
        var tool = CreateTool(new RecordingHandler(HttpStatusCode.OK, payload, "text/plain"));

        var result = await tool.Request("https://example.com/big", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsTrue();
        await Assert.That(result.Truncated).IsTrue();
        await Assert.That(result.Body!.Length).IsEqualTo(HttpTools.MaxResponseBodyBytes);
    }

    [Test]
    public async Task OmitsBinaryBodies()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.OK,
            [0x89, 0x50, 0x4E, 0x47, 0x00, 0x0D],
            "image/png");
        var tool = CreateTool(handler);

        var result = await tool.Request("https://example.com/logo.png", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsTrue();
        await Assert.That(result.Binary).IsTrue();
        await Assert.That(result.Body).IsNull();
        await Assert.That(result.Truncated).IsFalse();
    }

    [Test]
    public async Task TimesOutWhenTheServerDoesNotRespond()
    {
        var tool = CreateTool(new HangingHandler());

        var result = await tool.Request(
            "https://example.com/slow",
            timeoutSeconds: 1,
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.TimedOut).IsTrue();
        await Assert.That(result.Error).Contains("timed out");
    }

    [Test]
    public async Task SurfacesTransportFailures()
    {
        var tool = CreateTool(new ThrowingHandler());

        var result = await tool.Request("https://example.com/down", cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("Failed to perform HTTP request");
    }

    [Test]
    public async Task SaveAsWritesBinaryToTheWorkspace()
    {
        using var temp = TempWorkspace.Create();
        var payload = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x00, 0x0D };
        var tool = CreateTool(
            new RecordingHandler(HttpStatusCode.OK, payload, "image/png"),
            workspace: temp.Workspace);

        var result = await tool.Request(
            "https://example.com/logo.png",
            saveAs: "downloads/logo.png",
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Completed).IsTrue();
        await Assert.That(result.Binary).IsTrue();
        await Assert.That(result.Body).IsNull();
        await Assert.That(result.SavedPath).IsEqualTo("downloads/logo.png");
        await Assert.That(result.SavedBytes).IsEqualTo(payload.Length);
        await Assert.That(result.SavedTruncated).IsFalse();
        await Assert.That(await File.ReadAllBytesAsync(temp.PathOf("downloads", "logo.png"))).IsEquivalentTo(payload);
    }

    [Test]
    public async Task SaveAsWritesTextAndKeepsAnInlinePreview()
    {
        using var temp = TempWorkspace.Create();
        var tool = CreateTool(
            new RecordingHandler(HttpStatusCode.OK, "hello file", "text/plain"),
            workspace: temp.Workspace);

        var result = await tool.Request(
            "https://example.com/note.txt",
            saveAs: "note.txt",
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Error).IsNull();
        await Assert.That(result.Body).IsEqualTo("hello file");
        await Assert.That(result.SavedPath).IsEqualTo("note.txt");
        await Assert.That(result.SavedBytes).IsEqualTo(10);
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("note.txt"))).IsEqualTo("hello file");
    }

    [Test]
    public async Task SaveAsRefusesToEscapeTheWorkspace()
    {
        using var temp = TempWorkspace.Create();
        var handler = new RecordingHandler(HttpStatusCode.OK, "x");
        var tool = CreateTool(handler, workspace: temp.Workspace);

        var result = await tool.Request(
            "https://example.com/x",
            saveAs: "../escape.bin",
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("outside the agent workspace");
        await Assert.That(handler.LastRequest).IsNull();
    }

    [Test]
    public async Task SaveAsRefusesTheProtectedSystemFolder()
    {
        using var temp = TempWorkspace.Create();
        var handler = new RecordingHandler(HttpStatusCode.OK, "x");
        var tool = CreateTool(handler, workspace: temp.Workspace);

        var result = await tool.Request(
            "https://example.com/x",
            saveAs: "system/secret.bin",
            cancellationToken: CancellationToken.None);

        await Assert.That(result.Completed).IsFalse();
        await Assert.That(result.Error).Contains("'system/'");
        await Assert.That(handler.LastRequest).IsNull();
    }

    [Test]
    public async Task SaveAsRefusesAnExistingFileUnlessOverwriteIsSet()
    {
        using var temp = TempWorkspace.Create();
        await File.WriteAllTextAsync(temp.PathOf("kept.bin"), "original");
        var handler = new RecordingHandler(HttpStatusCode.OK, "new");
        var tool = CreateTool(handler, workspace: temp.Workspace);

        var refused = await tool.Request(
            "https://example.com/x",
            saveAs: "kept.bin",
            cancellationToken: CancellationToken.None);

        await Assert.That(refused.Completed).IsFalse();
        await Assert.That(refused.Error).Contains("already exists");
        await Assert.That(handler.LastRequest).IsNull();
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("kept.bin"))).IsEqualTo("original");

        var overwritten = await tool.Request(
            "https://example.com/x",
            saveAs: "kept.bin",
            overwrite: true,
            cancellationToken: CancellationToken.None);

        await Assert.That(overwritten.Error).IsNull();
        await Assert.That(overwritten.SavedPath).IsEqualTo("kept.bin");
        await Assert.That(await File.ReadAllTextAsync(temp.PathOf("kept.bin"))).IsEqualTo("new");
    }

    private static HttpTools CreateTool(
        HttpMessageHandler handler,
        string? agentId = "alice",
        AgentWorkspace? workspace = null)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(HttpTools.HttpClientName).Returns(new HttpClient(handler));
        return new HttpTools(
            new StubAgentWorkspaceLocator(workspace ?? new AgentWorkspace(agentId, "/tmp")),
            new PathExpander(),
            TestFileProtectionPolicies.AllowAll,
            factory,
            NullLogger<HttpTools>.Instance);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly byte[] _body;
        private readonly string? _contentType;

        public RecordingHandler(HttpStatusCode status, string body, string? contentType = "text/plain")
            : this(status, Encoding.UTF8.GetBytes(body), contentType)
        {
        }

        public RecordingHandler(HttpStatusCode status, byte[] body, string? contentType)
        {
            _status = status;
            _body = body;
            _contentType = contentType;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            var response = new HttpResponseMessage(_status)
            {
                Content = new ByteArrayContent(_body),
            };
            if (_contentType is not null)
            {
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_contentType);
            }

            return response;
        }
    }

    private sealed class HangingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("connection reset");
        }
    }
}
