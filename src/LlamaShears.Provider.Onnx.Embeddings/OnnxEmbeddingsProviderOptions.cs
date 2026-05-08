namespace LlamaShears.Provider.Onnx.Embeddings;

public sealed class OnnxEmbeddingsProviderOptions
{
    public string? ModelsRoot { get; set; }

    public Dictionary<string, OnnxModelOptions> Models { get; set; } = new(StringComparer.Ordinal);
}
