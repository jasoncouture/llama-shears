using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.OpenAI;

internal sealed record OpenAIResponseFragment(string Content) : IModelTextResponse;
