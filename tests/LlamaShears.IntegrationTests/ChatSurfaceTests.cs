using System.Net;
using LlamaShears.IntegrationTests.Hosting;

namespace LlamaShears.IntegrationTests;

public sealed class ChatSurfaceTests
{
    [Test]
    public async Task GetRootReturns200AndRendersTheChatShell()
    {
        await using var factory = new IsolatedAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        // The Chat page sets <PageTitle>LlamaShears</PageTitle>, which Blazor
        // renders into the document <title>. The page itself no longer has
        // an h1 — the old header bar was replaced by a hamburger nav drawer.
        await Assert.That(html).Contains("<title>LlamaShears</title>");
        await Assert.That(html).Contains("agent-select");
        await Assert.That(html).Contains("class=\"composer\"");
    }

    // Regression: the build-time heuristic that auto-aggregates the
    // Blazor framework JS into a project's static-web-assets manifest
    // requires the host project to contain at least one .razor file.
    // Our host has none — every component lives in the RCL — so the
    // manifest came up empty and `/_framework/blazor.web.js` 404'd. No
    // JS, no SignalR, no interactivity, no re-renders. Fix:
    // <RequiresAspNetWebAssets>true</RequiresAspNetWebAssets> on the
    // host csproj. The next two tests guard that fix.

    [Test]
    public async Task RegressionRootHtmlLinksToBlazorWebJsUnderFramework()
    {
        await using var factory = new IsolatedAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        // `@Assets["_framework/blazor.web.js"]` rewrites to a
        // fingerprinted path at render time; the bare path is the
        // canonical one. Either form is acceptable in the rendered HTML.
        await Assert.That(html).Contains("/_framework/blazor.web");
        await Assert.That(html).Contains(".js");
    }

    [Test]
    public async Task RegressionBlazorWebJsMustReturn200WhenHostHasNoRazorFiles()
    {
        await using var factory = new IsolatedAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/_framework/blazor.web.js", CancellationToken.None);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        var bytes = await response.Content.ReadAsByteArrayAsync(CancellationToken.None);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(contentType).IsEqualTo("text/javascript");
        await Assert.That(bytes.Length).IsGreaterThan(10_000);
    }

    [Test]
    public async Task RefreshButtonRendersTheArrowClockwiseIconBody()
    {
        // Guards the IconProvider <-> WebRootFileProvider <-> RCL static
        // assets path: if any of those break, the inner svg comes back
        // empty and the icon button is invisible (page still renders 200,
        // so a generic "page loads" test wouldn't catch it). The path
        // data below is verbatim from wwwroot/icons/arrow-clockwise.svg.
        await using var factory = new IsolatedAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(html).Contains("M8 3a5 5 0 1 0 4.546 2.914");
    }

    [Test]
    public async Task EmptyAgentsDirectoryRendersTheNoAgentsState()
    {
        await using var factory = new IsolatedAppFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/", CancellationToken.None);
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

        // Build the HTTP client first so the host starts; then nudge the
        // tick so the AgentManager picks up the seeded JSON.
        using var client = factory.CreateClient();
        await factory.WaitForAgentAsync("alpha");

        using var response = await client.GetAsync("/", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        // The <option> may carry a Blazor scoped-CSS attribute between
        // `value="alpha"` and the closing tag; assert on the two anchors.
        await Assert.That(html).Contains("value=\"alpha\"");
        await Assert.That(html).Contains(">alpha</option>");
    }

    [Test]
    public async Task RealOllamaProviderIsNotResolvableUnderTest()
    {
        await using var factory = new IsolatedAppFactory();
        // The host registers OllamaProviderFactory in production. Tests
        // strip every real IProviderFactory and substitute a stub, so an
        // agent referencing OLLAMA must fail to load — no live calls.
        factory.SeedAgent("ollama-only", """
            { "model": { "id": "OLLAMA/dummy" } }
            """);

        using var client = factory.CreateClient();
        await factory.TickAsync();

        // The agent never makes it into the manager's loaded set; the
        // build path logs a warning and skips it. Picker stays empty.
        using var response = await client.GetAsync("/", CancellationToken.None);
        var html = await response.Content.ReadAsStringAsync(CancellationToken.None);

        await Assert.That(html).DoesNotContain("ollama-only");
        await Assert.That(html).Contains("No agents loaded yet");
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
        // Give the agent loop a moment to do whatever it does on first
        // tick. With no user input pending and seed turn removed, the
        // model must not be invoked.
        await Task.Delay(150);

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
