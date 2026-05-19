using System.Net;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.IntegrationTests.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace LlamaShears.IntegrationTests.Web;

public sealed class ChatSurfaceTests
{
    private static IsolatedAppFactory _sharedFactory = null!;
    private static HttpClient _sharedClient = null!;

    [Before(Class)]
    public static void StartSharedHost()
    {
        _sharedFactory = new IsolatedAppFactory();
        _sharedClient = _sharedFactory.CreateClient();
    }

    [After(Class)]
    public static async Task StopSharedHost()
    {
        _sharedClient?.Dispose();
        if (_sharedFactory is not null)
        {
            await _sharedFactory.DisposeAsync();
        }
    }

    [Test]
    public async Task GetRootRedirectsToChat()
    {
        using var client = _sharedFactory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        using var response = await client.GetAsync("/", CancellationToken.None);

        await Assert.That((int)response.StatusCode).IsBetween(300, 399);
        await Assert.That(response.Headers.Location?.AbsolutePath).IsEqualTo("/chat");
    }

    [Test]
    public async Task GetChatPageReturns200AndRendersTheChatShell()
    {
        using var response = await _sharedClient.GetAsync("/chat", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(html).Contains("<title>LlamaShears</title>");
        await Assert.That(html).Contains("agent-select");
        await Assert.That(html).Contains("class=\"composer\"");
    }

    [Test]
    public async Task RegressionChatPageHtmlLinksToBlazorWebJsUnderFramework()
    {
        using var response = await _sharedClient.GetAsync("/chat", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(html).Contains("/_framework/blazor.web");
        await Assert.That(html).Contains(".js");
    }

    [Test]
    public async Task RegressionBlazorWebJsMustReturn200WhenHostHasNoRazorFiles()
    {
        using var response = await _sharedClient.GetAsync("/_framework/blazor.web.js", CancellationToken.None);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(contentType).IsEqualTo("text/javascript");
        await Assert.That(bytes.Length).IsGreaterThan(10_000);
    }

    [Test]
    public async Task RefreshButtonRendersTheArrowClockwiseIconBody()
    {
        using var response = await _sharedClient.GetAsync("/chat", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(html).Contains("M8 3a5 5 0 1 0 4.546 2.914");
    }

    [Test]
    public async Task EmptyAgentsDirectoryRendersTheNoAgentsState()
    {
        using var response = await _sharedClient.GetAsync("/chat", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(html).Contains("No agents loaded yet");
    }

    [Test]
    public async Task SeededAgentJsonAppearsInThePickerAfterATick()
    {
        await using var factory = new IsolatedAppFactory();
        factory.SeedAgent("alpha", """
            { "model": { "id": "TEST/dummy" } }
            """);

        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync("alpha");

        using var response = await client.GetAsync("/chat", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(html).Contains("value=\"alpha\"");
        await Assert.That(html).Contains(">alpha</option>");
    }

    [Test]
    public async Task RealOllamaProviderIsNotResolvableUnderTest()
    {
        await using var factory = new IsolatedAppFactory();
        using var _ = factory.CreateClient();

        var providers = factory.Services.GetServices<IProviderFactory>().ToList();

        await Assert.That(providers.Count).IsEqualTo(1);
        await Assert.That(providers[0]).IsSameReferenceAs(factory.ProviderFactory);
        await Assert.That(providers.Any(p => string.Equals(p.Name, "OLLAMA", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    public async Task StubModelIsNotInvokedOnAgentStartupWithNoInput()
    {
        await using var factory = new IsolatedAppFactory();
        factory.SeedAgent("alpha", """
            { "model": { "id": "TEST/dummy" } }
            """);

        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync("alpha");

        await Assert.That(factory.Model.InvocationCount).IsEqualTo(0);
    }

    [Test]
    public async Task TwoFactoriesUseIsolatedDataRoots()
    {
        await using var a = new IsolatedAppFactory();
        await using var b = new IsolatedAppFactory();

        await Assert.That(a.DataRoot).IsNotEqualTo(b.DataRoot);
        await Assert.That(a.AgentsRoot).IsNotEqualTo(b.AgentsRoot);
        await Assert.That(Directory.Exists(a.AgentsRoot)).IsTrue();
        await Assert.That(Directory.Exists(b.AgentsRoot)).IsTrue();
    }

    [Test]
    public async Task DisposingFactoryRemovesItsTempDataRoot()
    {
        var factory = new IsolatedAppFactory();
        var dataRoot = factory.DataRoot;
        await Assert.That(Directory.Exists(dataRoot)).IsTrue();

        await factory.DisposeAsync();

        await Assert.That(Directory.Exists(dataRoot)).IsFalse();
    }

    [Test]
    public async Task NoTestEverResolvesToTheDeveloperDefaultDataRoot()
    {
        await using var factory = new IsolatedAppFactory();

        var defaultDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".llama-shears");

        await Assert.That(factory.DataRoot.StartsWith(defaultDataRoot, StringComparison.Ordinal))
            .IsFalse();
        await Assert.That(factory.AgentsRoot.StartsWith(defaultDataRoot, StringComparison.Ordinal))
            .IsFalse();
    }
}
