using LlamaShears.Core;
using LlamaShears.Core.Abstractions.Caching;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.SystemPrompt;
using LlamaShears.Core.Caching;
using LlamaShears.Core.Paths;
using LlamaShears.Core.SystemPrompt;
using LlamaShears.Core.Templating;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.SystemPrompt;

public sealed class FilesystemSystemPromptProviderTests
{
    private static readonly SystemPromptTemplateParameters _emptyParameters = new();

    [Test]
    public async Task NamedTemplateInWorkspaceIsReturned()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync("MINIMAL", "minimal-from-workspace");

        var body = await fixture.Provider.GetAsync("MINIMAL", _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("minimal-from-workspace");
    }

    [Test]
    public async Task FallsBackToWorkspaceDefaultWhenNamedTemplateMissing()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync("DEFAULT", "default-from-workspace");

        var body = await fixture.Provider.GetAsync("MINIMAL", _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("default-from-workspace");
    }

    [Test]
    public async Task FallsBackToBundledNamedWhenWorkspaceMissingBoth()
    {
        using var fixture = new Fixture();
        await fixture.WriteBundledTemplateAsync("MINIMAL", "minimal-from-bundled");

        var body = await fixture.Provider.GetAsync("MINIMAL", _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("minimal-from-bundled");
    }

    [Test]
    public async Task FallsBackToBundledDefaultWhenEverythingElseMissing()
    {
        using var fixture = new Fixture();
        await fixture.WriteBundledTemplateAsync("DEFAULT", "default-from-bundled");

        var body = await fixture.Provider.GetAsync("MINIMAL", _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("default-from-bundled");
    }

    [Test]
    public async Task WorkspaceWinsOverBundledWhenBothPresent()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync("MINIMAL", "minimal-from-workspace");
        await fixture.WriteBundledTemplateAsync("MINIMAL", "minimal-from-bundled");

        var body = await fixture.Provider.GetAsync("MINIMAL", _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("minimal-from-workspace");
    }

    [Test]
    public async Task WorkspaceDefaultWinsOverBundledNamed()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync("DEFAULT", "default-from-workspace");
        await fixture.WriteBundledTemplateAsync("MINIMAL", "minimal-from-bundled");

        var body = await fixture.Provider.GetAsync("MINIMAL", _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("default-from-workspace");
    }

    [Test]
    public async Task ThrowsWhenNoCandidateExists()
    {
        using var fixture = new Fixture();

        await Assert.That(async () => await fixture.Provider.GetAsync("MINIMAL", _emptyParameters, CancellationToken.None))
            .Throws<FileNotFoundException>();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task NullOrBlankNameDefaultsToDefault(string? name)
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync("DEFAULT", "default-from-workspace");

        var body = await fixture.Provider.GetAsync(name, _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("default-from-workspace");
    }

    [Test]
    [Arguments("foo/bar")]
    [Arguments("foo\\bar")]
    [Arguments("/absolute")]
    [Arguments("..\\escape")]
    public async Task NameContainingPathSeparatorThrows(string name)
    {
        using var fixture = new Fixture();

        await Assert.That(async () => await fixture.Provider.GetAsync(name, _emptyParameters, CancellationToken.None))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task ParametersAreBoundIntoTemplate()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync(
            "DEFAULT",
            "agent={{ agent_id }} ws={{ workspace_path }}");

        var body = await fixture.Provider.GetAsync(
            "DEFAULT",
            new SystemPromptTemplateParameters(AgentId: "alice", WorkspacePath: "/tmp/work"),
            CancellationToken.None);

        await Assert.That(body).IsEqualTo("agent=alice ws=/tmp/work");
    }

    [Test]
    public async Task WorkspaceFilesAreLoadedAndRenderedInOrder()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceFileAsync("BOOTSTRAP.md", "boot-body");
        await fixture.WriteWorkspaceFileAsync("IDENTITY.md", "ident-body");
        await fixture.WriteWorkspaceFileAsync("SOUL.md", "soul-body");
        await fixture.WriteWorkspaceTemplateAsync(
            "DEFAULT",
            "{{- for file in files -}}\n[{{ file.name }}={{ file.content }}]\n{{- end -}}");

        var body = await fixture.Provider.GetAsync(
            "DEFAULT",
            new SystemPromptTemplateParameters(WorkspacePath: fixture.WorkspaceFilesDir),
            CancellationToken.None);

        await Assert.That(body).IsEqualTo("[BOOTSTRAP.md=boot-body][IDENTITY.md=ident-body][SOUL.md=soul-body]");
    }

    [Test]
    public async Task MissingWorkspaceFilesAreSilentlySkipped()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceFileAsync("IDENTITY.md", "only-identity");
        await fixture.WriteWorkspaceTemplateAsync(
            "DEFAULT",
            "{{ files.size }}|{{- for file in files -}}{{ file.name }}{{- end -}}");

        var body = await fixture.Provider.GetAsync(
            "DEFAULT",
            new SystemPromptTemplateParameters(WorkspacePath: fixture.WorkspaceFilesDir),
            CancellationToken.None);

        await Assert.That(body).IsEqualTo("1|IDENTITY.md");
    }

    [Test]
    public async Task NoWorkspacePathYieldsEmptyFiles()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync("DEFAULT", "{{ files.size }}");

        var body = await fixture.Provider.GetAsync("DEFAULT", _emptyParameters, CancellationToken.None);

        await Assert.That(body).IsEqualTo("0");
    }

    [Test]
    public async Task NullParameterRendersAsEmptyToken()
    {
        using var fixture = new Fixture();
        await fixture.WriteWorkspaceTemplateAsync(
            "DEFAULT",
            "agent={{ agent_id }} ws={{ workspace_path }}");

        var body = await fixture.Provider.GetAsync(
            "DEFAULT",
            new SystemPromptTemplateParameters(AgentId: "alice"),
            CancellationToken.None);

        await Assert.That(body).IsEqualTo("agent=alice ws=");
    }

    private sealed class Fixture : IDisposable
    {
        private readonly string _root;
        private readonly MemoryCache _memory;
        private readonly FileParserCache<TemplateRenderer> _rendererCache;

        public Fixture()
        {
            _root = Path.Combine(Path.GetTempPath(), $"llamashears-syspromptprov-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_root);
            WorkspaceSystemDir = Path.Combine(_root, "templates", "workspace", "system");
            BundledRoot = Path.Combine(_root, "bundled");
            WorkspaceFilesDir = Path.Combine(_root, "agent-workspace");

            var pathsOptions = Options.Create(new ShearsPathsOptions
            {
                DataRoot = Path.Combine(_root, "data"),
                TemplatesRoot = Path.Combine(_root, "templates"),
            });
            IShearsPaths paths = new ShearsPaths(pathsOptions);

            _memory = new MemoryCache(new MemoryCacheOptions());
            IShearsCache<TemplateRenderer> shears = new ShearsCache<TemplateRenderer>(_memory);
            var fpcOptions = new TestOptionsMonitor<FileParserCacheOptions>(
                new FileParserCacheOptions { TimeToLive = TimeSpan.FromMinutes(10) });
            _rendererCache = new FileParserCache<TemplateRenderer>(shears, fpcOptions);
            var renderer = new TemplateRenderer(_rendererCache);

            var providerOptions = Options.Create(new FilesystemSystemPromptOptions { BundledRoot = BundledRoot });
            Provider = new FilesystemSystemPromptProvider(paths, renderer, providerOptions);
        }

        public string WorkspaceSystemDir { get; }

        public string BundledRoot { get; }

        public string WorkspaceFilesDir { get; }

        public FilesystemSystemPromptProvider Provider { get; }

        public Task WriteWorkspaceTemplateAsync(string name, string content)
        {
            Directory.CreateDirectory(WorkspaceSystemDir);
            return File.WriteAllTextAsync(Path.Combine(WorkspaceSystemDir, $"{name}.md"), content);
        }

        public Task WriteBundledTemplateAsync(string name, string content)
        {
            Directory.CreateDirectory(BundledRoot);
            return File.WriteAllTextAsync(Path.Combine(BundledRoot, $"{name}.md"), content);
        }

        public Task WriteWorkspaceFileAsync(string fileName, string content)
        {
            Directory.CreateDirectory(WorkspaceFilesDir);
            return File.WriteAllTextAsync(Path.Combine(WorkspaceFilesDir, fileName), content);
        }

        public void Dispose()
        {
            _rendererCache.Dispose();
            _memory.Dispose();
            try
            {
                Directory.Delete(_root, recursive: true);
            }
            catch (DirectoryNotFoundException)
            {
            }
        }
    }

    private sealed class TestOptionsMonitor<TOptions> : IOptionsMonitor<TOptions>
    {
        private readonly TOptions _value;

        public TestOptionsMonitor(TOptions value)
        {
            _value = value;
        }

        public TOptions CurrentValue => _value;

        public TOptions Get(string? name) => _value;

        public IDisposable OnChange(Action<TOptions, string?> listener) => new NoopDisposable();

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
