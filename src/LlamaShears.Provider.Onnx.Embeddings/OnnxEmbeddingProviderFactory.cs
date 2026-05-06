using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Options;

namespace LlamaShears.Provider.Onnx.Embeddings;

public sealed class OnnxEmbeddingProviderFactory : IEmbeddingProviderFactory, IDisposable
{
    public const string ProviderName = "onnx";

    private readonly IOptionsMonitor<OnnxEmbeddingsProviderOptions> _options;
    private readonly ConcurrentDictionary<string, OnnxEmbeddingModel> _models = new(StringComparer.Ordinal);

    public OnnxEmbeddingProviderFactory(IOptionsMonitor<OnnxEmbeddingsProviderOptions> options)
    {
        _options = options;
    }

    public string Name => ProviderName;

    public async IAsyncEnumerable<ModelInfo> ListModelsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        foreach (var (id, model) in _options.CurrentValue.Models)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ModelInfo(
                ModelId: id,
                DisplayName: model.DisplayName ?? id,
                Description: model.Description,
                SupportedInputs: SupportedInputType.Text,
                SupportsReasoning: false,
                MaxContextWindow: model.MaxSequenceLength);
        }
    }

    public IEmbeddingModel CreateModel(ModelConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuration.ModelId);
        return _models.GetOrAdd(configuration.ModelId, BuildModel);
    }

    private OnnxEmbeddingModel BuildModel(string modelId)
    {
        var snapshot = _options.CurrentValue;
        if (!snapshot.Models.TryGetValue(modelId, out var modelOptions))
        {
            throw new InvalidOperationException(
                $"No ONNX embedding model is configured with id '{modelId}'. Add it to the provider's Models map.");
        }
        if (string.IsNullOrWhiteSpace(modelOptions.ModelPath))
        {
            throw new InvalidOperationException(
                $"ONNX embedding model '{modelId}' is missing a ModelPath.");
        }
        if (string.IsNullOrWhiteSpace(modelOptions.VocabPath))
        {
            throw new InvalidOperationException(
                $"ONNX embedding model '{modelId}' is missing a VocabPath (the BERT vocab.txt).");
        }

        var rootRaw = string.IsNullOrWhiteSpace(snapshot.ModelsRoot)
            ? OnnxEmbeddingsProviderOptions.DefaultModelsRoot
            : snapshot.ModelsRoot!;
        var root = Path.GetFullPath(rootRaw);
        var modelFullPath = ResolveSandboxed(root, modelOptions.ModelPath, modelId, nameof(modelOptions.ModelPath));
        var vocabFullPath = ResolveSandboxed(root, modelOptions.VocabPath, modelId, nameof(modelOptions.VocabPath));
        if (!File.Exists(modelFullPath))
        {
            throw new FileNotFoundException(
                $"ONNX embedding model '{modelId}' file not found at '{modelFullPath}'. Place it under the configured ModelsRoot ('{root}').",
                modelFullPath);
        }
        if (!File.Exists(vocabFullPath))
        {
            throw new FileNotFoundException(
                $"ONNX embedding model '{modelId}' vocab file not found at '{vocabFullPath}'. Place it under the configured ModelsRoot ('{root}').",
                vocabFullPath);
        }
        return new OnnxEmbeddingModel(modelOptions, modelFullPath, vocabFullPath);
    }

    private static string ResolveSandboxed(string root, string relativePath, string modelId, string field)
    {
        if (Path.IsPathFullyQualified(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException(
                $"ONNX embedding model '{modelId}' has an absolute {field} ('{relativePath}'); " +
                $"paths must be relative to ModelsRoot ('{root}').");
        }
        var combined = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ONNX embedding model '{modelId}' {field} ('{relativePath}') escapes the configured ModelsRoot ('{root}').");
        }
        return combined;
    }

    public void Dispose()
    {
        foreach (var model in _models.Values)
        {
            model.Dispose();
        }
        _models.Clear();
    }
}
