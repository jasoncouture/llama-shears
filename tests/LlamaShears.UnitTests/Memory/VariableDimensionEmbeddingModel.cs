using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Memory;

internal sealed class VariableDimensionEmbeddingModel : IEmbeddingModel
{
    private readonly Func<int> _getDimensions;

    public VariableDimensionEmbeddingModel(Func<int> getDimensions)
    {
        _getDimensions = getDimensions;
    }

    public ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken)
        => ValueTask.FromResult(Embed(text));

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        var results = new ReadOnlyMemory<float>[texts.Count];
        for (var i = 0; i < texts.Count; i++)
        {
            results[i] = Embed(texts[i]);
        }
        return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
    }

    // Same hash-shaped scheme as StubEmbeddingModel, but the dimension
    // comes from the supplied accessor so a test can change it between
    // calls (simulating a swap of the underlying embedding model).
    private ReadOnlyMemory<float> Embed(string text)
    {
        var dim = _getDimensions();
        var v = new float[dim];
        for (var i = 0; i < text.Length; i++)
        {
            var slot = (uint)text[i] % dim;
            v[slot] += 1f;
        }
        return v;
    }
}
