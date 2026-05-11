using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class HangingLanguageModel : ILanguageModel
{
    private readonly TaskCompletionSource _invoked =
        new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitForInvocationAsync(TimeSpan timeout) =>
        _invoked.Task.WaitAsync(timeout);

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _invoked.TrySetResult();
        await Task.Delay(Timeout.Infinite, cancellationToken);
        yield break;
    }
}
