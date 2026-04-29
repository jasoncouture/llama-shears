using Microsoft.Extensions.ObjectPool;
using OllamaSharp.Models.Chat;

namespace LlamaShears.Provider.Ollama;

internal sealed class MessageListPooledObjectPolicy : PooledObjectPolicy<List<Message>>
{
    private const int InitialCapacity = 16;
    private const int MaximumRetainedCapacity = 1024;

    public override List<Message> Create() => new(InitialCapacity);

    public override bool Return(List<Message> obj)
    {
        if (obj.Capacity > MaximumRetainedCapacity)
        {
            return false;
        }

        obj.Clear();
        return true;
    }
}
