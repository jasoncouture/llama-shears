using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Paths;
using LlamaShears.Core.Abstractions.Provider;
using Microsoft.Extensions.Options;

namespace LlamaShears.Provider.Onnx.Embeddings;

public sealed class OnnxEmbeddingProviderFactory : IEmbeddingProviderFactory, IDisposable
{
    public const string ProviderName = "onnx";

    private readonly IOptionsMonitor<OnnxEmbeddingsProviderOptions> _options;
    private readonly IShearsPaths _paths;
    private readonly ConcurrentDictionary<string, OnnxEmbeddingModel> _models = new(StringComparer.Ordinal);

    public OnnxEmbeddingProviderFactory(
        IOptionsMonitor<OnnxEmbeddingsProviderOptions> options,
        IShearsPaths paths)
    {
        _options = options;
        _paths = paths;
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

        var rootRaw = string.IsNullOrWhiteSpace(snapshot.ModelsRoot)
            ? Path.Combine(_paths.GetPath(PathKind.Data), "models", "onnx", "embeddings")
            : snapshot.ModelsRoot;
        var root = Directory.CreateDirectory(rootRaw).FullName;
        var modelDir = ResolveSandboxedDirectory(root, modelId);
        if (!Directory.Exists(modelDir))
        {
            throw new DirectoryNotFoundException(
                $"ONNX embedding model '{modelId}' directory not found at '{modelDir}'. Create it under the configured ModelsRoot ('{root}') and place exactly one *.onnx and one *.txt file inside.");
        }

        var modelFullPath = RequireExactlyOne(modelDir, "*.onnx", modelId);
        var vocabFullPath = RequireExactlyOne(modelDir, "*.txt", modelId);
        return new OnnxEmbeddingModel(modelOptions, modelFullPath, vocabFullPath);
    }

    private static string ResolveSandboxedDirectory(string root, string modelId)
    {
        if (Path.IsPathFullyQualified(modelId) || Path.IsPathRooted(modelId))
        {
            throw new InvalidOperationException(
                $"ONNX embedding model id '{modelId}' is an absolute path; ids must be relative subdirectory names under ModelsRoot ('{root}').");
        }
        var combined = Path.GetFullPath(Path.Combine(root, modelId));
        var rootWithSep = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(rootWithSep, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"ONNX embedding model id '{modelId}' resolves outside the configured ModelsRoot ('{root}').");
        }
        return combined;
    }

    private static string RequireExactlyOne(string directory, string searchPattern, string modelId)
    {
        var matches = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
        if (matches.Length == 0)
        {
            throw new FileNotFoundException(
                $"ONNX embedding model '{modelId}' is missing a {searchPattern} file in '{directory}'. Place exactly one such file there.");
        }
        if (matches.Length > 1)
        {
            throw new InvalidOperationException(
                $"ONNX embedding model '{modelId}' has multiple {searchPattern} files in '{directory}' ({string.Join(", ", matches.Select(Path.GetFileName))}); only one is allowed.");
        }
        return matches[0];
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
