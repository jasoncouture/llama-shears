using LlamaShears.Core.Abstractions.Provider;

namespace LlamaShears.Provider.OpenAI;

internal sealed record OpenAiThoughtFragment(string Content) : IModelThoughtResponse;
