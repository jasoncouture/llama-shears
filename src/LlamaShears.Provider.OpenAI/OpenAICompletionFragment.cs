using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.OpenAI;

internal sealed record OpenAICompletionFragment(int TokenCount) : IModelCompletionResponse;
