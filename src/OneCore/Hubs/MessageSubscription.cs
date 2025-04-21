namespace CoreOne.Hubs;

internal class MessageSubscription<TEvent>(Func<TEvent, Task> deliveryAction, Predicate<TEvent>? messageFilter) : IHubSubscription where TEvent : IHubMessage
{
    protected Func<TEvent, Task> DeliveryAction { get; } = deliveryAction;
    protected Predicate<TEvent> MessageFilter { get; } = messageFilter ?? (p => true);

    public async ValueTask Deliver(IHubMessage message)
    {
        if (message is TEvent msg && MessageFilter.Invoke(msg) && DeliveryAction is not null)
            await DeliveryAction.Invoke(msg);
    }
}
