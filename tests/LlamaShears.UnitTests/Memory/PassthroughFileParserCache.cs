using LlamaShears.Core.Abstractions.Caching;

namespace LlamaShears.UnitTests.Memory;

internal sealed class PassthroughFileParserCache<T> : IFileParserCache<T> where T : class
{
    public async ValueTask<TItem?> GetOrParseAsync<TItem, TState>(
        string path,
        TState state,
        Func<Stream?, TState, CancellationToken, ValueTask<TItem?>> parser,
        CancellationToken cancellationToken)
        where TItem : class
    {
        if (File.Exists(path))
        {
            await using var stream = File.OpenRead(path);
            return await parser(stream, state, cancellationToken).ConfigureAwait(false);
        }
        return await parser(null, state, cancellationToken).ConfigureAwait(false);
    }
}
