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
    public async Task AbsoluteModelPathIsRejected()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["abs"] = new OnnxModelOptions
            {
                ModelPath = Path.Combine(Path.GetTempPath(), "model.onnx"),
                VocabPath = "vocab.txt",
            };
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration("abs")));

        await Assert.That(ex.Message).Contains("absolute");
    }

    [Test]
    public async Task PathEscapingModelsRootIsRejected()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["escape"] = new OnnxModelOptions
            {
                ModelPath = Path.Combine("..", "..", "etc", "passwd"),
                VocabPath = "vocab.txt",
            };
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration("escape")));

        await Assert.That(ex.Message).Contains("escapes");
    }

    [Test]
    public async Task MissingFileGivesClearError()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["missing"] = new OnnxModelOptions
            {
                ModelPath = "missing-model.onnx",
                VocabPath = "vocab.txt",
            };
        });

        var ex = Assert.Throws<FileNotFoundException>(() =>
            fixture.Factory.CreateModel(new ModelConfiguration("missing")));

        await Assert.That(ex.Message).Contains("missing-model.onnx");
    }

    [Test]
    public async Task ListModelsAsyncEnumeratesConfiguredEntries()
    {
        using var fixture = OnnxFactoryFixture.Create(opts =>
        {
            opts.Models["a"] = new OnnxModelOptions { ModelPath = "a.onnx", VocabPath = "a.txt" };
            opts.Models["b"] = new OnnxModelOptions { ModelPath = "b.onnx", VocabPath = "b.txt", DisplayName = "Beta" };
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
            return new OnnxFactoryFixture
            {
                Factory = new OnnxEmbeddingProviderFactory(monitor),
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
}
