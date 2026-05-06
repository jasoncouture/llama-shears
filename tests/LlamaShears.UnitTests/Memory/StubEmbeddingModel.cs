using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Memory;

internal sealed class StubEmbeddingModel : IEmbeddingModel
{
    private const int Dimensions = 16;

    public ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, EmbeddingPurpose purpose, CancellationToken cancellationToken)
        => ValueTask.FromResult(Embed(text));

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken)
    {
        var results = new ReadOnlyMemory<float>[texts.Count];
        for (var i = 0; i < texts.Count; i++)
        {
            results[i] = Embed(texts[i]);
        }
        return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(results);
    }

    // Deterministic, hash-shaped embedding. Identical text → identical
    // vector; lexically similar text → similar vector. Coarse but
    // sufficient to exercise cosine ranking in tests. Purpose is ignored
    // — the stub doesn't model asymmetric encoders.
    private static ReadOnlyMemory<float> Embed(string text)
    {
        var v = new float[Dimensions];
        for (var i = 0; i < text.Length; i++)
        {
            var slot = (uint)text[i] % Dimensions;
            v[slot] += 1f;
        }
        return v;
    }
}
