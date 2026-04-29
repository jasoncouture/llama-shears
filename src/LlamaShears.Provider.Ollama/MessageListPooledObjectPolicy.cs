using Microsoft.Extensions.ObjectPool;
using OllamaSharp.Models.Chat;

namespace LlamaShears.Provider.Ollama;

/// <summary>
/// Pool policy for <see cref="List{Message}"/> instances used to build
/// <see cref="ChatRequest"/> message arrays. Lists are cleared on return
/// and discarded if they have grown beyond <see cref="MaximumRetainedCapacity"/>.
/// </summary>
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
