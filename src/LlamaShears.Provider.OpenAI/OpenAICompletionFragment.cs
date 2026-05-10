using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.OpenAI;

internal sealed record OpenAiCompletionFragment(int TokenCount) : IModelCompletionResponse;
