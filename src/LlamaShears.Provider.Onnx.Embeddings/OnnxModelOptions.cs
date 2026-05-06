namespace LlamaShears.Provider.Onnx.Embeddings;

public sealed class OnnxModelOptions
{
    public int MaxSequenceLength { get; set; } = 256;
    public bool LowerCase { get; set; } = true;
    public OnnxPoolingStrategy Pooling { get; set; } = OnnxPoolingStrategy.Mean;
    public bool Normalize { get; set; } = true;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}
