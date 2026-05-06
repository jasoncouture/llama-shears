namespace LlamaShears.Core.Abstractions.Events;

[Flags]
public enum EventDeliveryMask
{
    None = 0,
    FireAndForget = 1,
    Awaited = 2,
    Both = FireAndForget | Awaited,
}
