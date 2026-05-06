using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class ScriptedLanguageModel : ILanguageModel
{
    private readonly IReadOnlyList<IModelResponseFragment> _fragments;
    private int _invocations;

    public ScriptedLanguageModel(params string[] textFragments)
    {
        _fragments = [.. textFragments.Select(IModelResponseFragment (f) => new TextFragment(f))];
    }

    private ScriptedLanguageModel(IReadOnlyList<IModelResponseFragment> fragments)
    {
        _fragments = fragments;
    }

    public static ScriptedLanguageModel WithText(params string[] fragments)
        => new([.. fragments.Select(IModelResponseFragment (f) => new TextFragment(f))]);

    public static ScriptedLanguageModel WithThoughtThenText(string[] thoughts, string[] text)
        => new([
            .. thoughts.Select(IModelResponseFragment (t) => new ThoughtFragment(t)),
            .. text.Select(IModelResponseFragment (t) => new TextFragment(t)),
        ]);

    public int PromptInvocations => Volatile.Read(ref _invocations);

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

    private sealed record TextFragment(string Content) : IModelTextResponse;

    private sealed record ThoughtFragment(string Content) : IModelThoughtResponse;
}
