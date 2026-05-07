using System.Runtime.CompilerServices;
using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.UnitTests.Agent.Core;

internal sealed class HangingLanguageModel : ILanguageModel
{
    private readonly TaskCompletionSource _invoked = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitForInvocationAsync(TimeSpan timeout) =>
        _invoked.Task.WaitAsync(timeout);

    public async IAsyncEnumerable<IModelResponseFragment> PromptAsync(
        ModelPrompt prompt,
        PromptOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _invoked.TrySetResult();
        // Block until the caller cancels. ThrowIfCancellationRequested
        // surfaces OperationCanceledException, which the agent's run-loop
        // converts to "turn interrupted".
        await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
        yield break;
    }
}
