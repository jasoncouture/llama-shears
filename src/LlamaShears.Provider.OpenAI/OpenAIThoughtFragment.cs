using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.OpenAI;

internal sealed record OpenAIThoughtFragment(string Content) : IModelThoughtResponse;
