using System.Runtime.CompilerServices;
using LlamaShears.Provider.Abstractions;

namespace LlamaShears.IntegrationTests.Hosting;

/// <summary>
/// Deterministic <see cref="ILanguageModel"/> used by the test host.
/// Returns a configurable, canned response so the agent loop completes
/// without ever reaching the network. Tests that need specific output
/// can pass their own <c>fragments</c>; the default is a single text
/// fragment that's recognisable in logs and HTML.
/// <para>
/// Real provider factories (e.g. Ollama) are removed from the test
/// container, so this is the only model the agent can possibly receive
/// — accidental live calls are structurally impossible.
/// </para>
/// </summary>
public sealed class StubLanguageModel : ILanguageModel
{
    public const string DefaultResponse = "[test stub response]";

    private readonly IReadOnlyList<IModelResponseFragment> _fragments;
    private int _invocations;

    public StubLanguageModel()
        : this([new TextFragment(DefaultResponse)])
    {
    }

    public StubLanguageModel(IReadOnlyList<IModelResponseFragment> fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);
        _fragments = fragments;
    }

    public int InvocationCount => Volatile.Read(ref _invocations);

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _invocations);
        foreach (var fragment in _fragments)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return fragment;
            await Task.Yield();
        }
    }

    public static StubLanguageModel WithText(params string[] fragments)
        => new([.. fragments.Select(IModelResponseFragment (f) => new TextFragment(f))]);

    private sealed record TextFragment(string Content) : IModelTextResponse;
}
