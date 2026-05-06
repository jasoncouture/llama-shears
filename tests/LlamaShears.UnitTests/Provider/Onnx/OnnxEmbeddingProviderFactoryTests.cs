using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using LlamaShears.Provider.Onnx.Embeddings;
using Microsoft.Extensions.Options;

namespace LlamaShears.UnitTests.Provider.Onnx;

public sealed class OnnxEmbeddingProviderFactoryTests
{
    [Test]
    public async Task UnknownModelIdThrowsInvalidOperation()
    {
        using var fixture = OnnxFactoryFixture.Create();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration("does-not-exist")));

        await Assert.That(ex.Message).Contains("does-not-exist");
    }

    [Test]
    public async Task AbsoluteModelIdIsRejected()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models[Path.Combine(Path.GetTempPath(), "abs")] = new OnnxModelOptions();
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration(Path.Combine(Path.GetTempPath(), "abs"))));

        await Assert.That(ex.Message).Contains("absolute");
    }

    [Test]
    public async Task ModelIdEscapingModelsRootIsRejected()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models[Path.Combine("..", "..", "etc")] = new OnnxModelOptions();
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration(Path.Combine("..", "..", "etc"))));

        await Assert.That(ex.Message).Contains("outside");
    }

    [Test]
    public async Task MissingDirectoryGivesClearError()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["missing"] = new OnnxModelOptions();
        });

        var ex = Assert.Throws<DirectoryNotFoundException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration("missing")));

        await Assert.That(ex.Message).Contains("missing");
    }

    [Test]
    public async Task MissingOnnxFileGivesClearError()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["no-onnx"] = new OnnxModelOptions();
        });
        var dir = Directory.CreateDirectory(Path.Combine(fixture.Root, "no-onnx")).FullName;
        await File.WriteAllTextAsync(Path.Combine(dir, "vocab.txt"), "[CLS]\n");

        var ex = Assert.Throws<FileNotFoundException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration("no-onnx")));

        await Assert.That(ex.Message).Contains("*.onnx");
    }

    [Test]
    public async Task MultipleOnnxFilesGiveClearError()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["dupes"] = new OnnxModelOptions();
        });
        var dir = Directory.CreateDirectory(Path.Combine(fixture.Root, "dupes")).FullName;
        await File.WriteAllBytesAsync(Path.Combine(dir, "a.onnx"), [0]);
        await File.WriteAllBytesAsync(Path.Combine(dir, "b.onnx"), [0]);
        await File.WriteAllTextAsync(Path.Combine(dir, "vocab.txt"), "[CLS]\n");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration("dupes")));

        await Assert.That(ex.Message).Contains("only one");
    }

    [Test]
    public async Task ListModelsAsyncEnumeratesConfiguredEntries()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["a"] = new OnnxModelOptions();
            opts.Models["b"] = new OnnxModelOptions { DisplayName = "Beta" };
        });

        var ids = new List<string>();
        await foreach (var info in fixture.Factory.ListModelsAsync(CancellationToken.None))
        {
            ids.Add(info.ModelId);
        }

        await Assert.That(ids).Contains("a");
        await Assert.That(ids).Contains("b");
    }

    private sealed class OnnxFactoryFixture : IDisposable
    {
        public required OnnxEmbeddingProviderFactory Factory { get; init; }
        public required string Root { get; init; }

        public static OnnxFactoryFixture Create(Action<OnnxEmbeddingsProviderOptions>? configure = null)
        {
            var root = Path.Combine(Path.GetTempPath(), "llamashears-onnx-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            var opts = new OnnxEmbeddingsProviderOptions { ModelsRoot = root };
            configure?.Invoke(opts);
            var monitor = new StaticOptionsMonitor<OnnxEmbeddingsProviderOptions>(opts);
            // ModelsRoot is set, so the IShearsPaths fallback is unused
            // here. A stub that throws on GetPath proves that.
            var paths = new ThrowingPaths();
            return new OnnxFactoryFixture
            {
                Factory = new OnnxEmbeddingProviderFactory(monitor, paths),
                Root = root,
            };
        }

        public void Dispose()
        {
            Factory.Dispose();
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch (IOException)
            {
            }
        }
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T value) => CurrentValue = value;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    private sealed class ThrowingPaths : IShearsPaths
    {
        public string GetPath(PathKind kind, string? subpath = null, bool ensureExists = false)
            => throw new InvalidOperationException("IShearsPaths.GetPath should not be reached when ModelsRoot is explicitly set.");
    }
}
