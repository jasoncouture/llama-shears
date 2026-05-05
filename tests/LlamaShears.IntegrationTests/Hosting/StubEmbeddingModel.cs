using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.IntegrationTests.Hosting;

internal sealed class StubEmbeddingModel : IEmbeddingModel
{
    private const int Dimensions = 16;

    public ValueTask<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken)
        => ValueTask.FromResult(Embed(text));

    public ValueTask<IReadOnlyList<ReadOnlyMemory<float>>> EmbedAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken)
    {
        var v = new ReadOnlyMemory<float>[texts.Count];
        for (var i = 0; i < texts.Count; i++)
        {
            v[i] = Embed(texts[i]);
        }
        return ValueTask.FromResult<IReadOnlyList<ReadOnlyMemory<float>>>(v);
    }

    private static ReadOnlyMemory<float> Embed(string text)
    {
        var v = new float[Dimensions];
        for (var i = 0; i < text.Length; i++)
        {
            v[(uint)text[i] % Dimensions] += 1f;
        }
        return v;
    }
}
