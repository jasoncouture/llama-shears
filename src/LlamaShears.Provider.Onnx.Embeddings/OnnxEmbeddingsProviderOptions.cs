namespace LlamaShears.Provider.Onnx.Embeddings;

public sealed class OnnxEmbeddingsProviderOptions
{
    // Optional override of the on-disk ModelsRoot. When unset (the
    // common case), the provider computes <Data>/models/onnx/embeddings
    // from IShearsPaths.
    public string? ModelsRoot { get; set; }

    public Dictionary<string, OnnxModelOptions> Models { get; set; } = new(StringComparer.Ordinal);
}
