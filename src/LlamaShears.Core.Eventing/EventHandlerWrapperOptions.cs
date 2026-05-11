using LlamaShears.Core.Abstractions.Events;

namespace LlamaShears.Core.Eventing;

internal sealed record EventHandlerWrapperOptions(string? Pattern, EventDeliveryMode DeliveryMode);
