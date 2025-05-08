namespace CoreOne.Hubs;

internal class MessageSubscription<TEvent>(Func<TEvent, Task> deliveryAction, Predicate<TEvent>? filter) : IHubSubscription where TEvent : IHubMessage
{
    public async ValueTask Deliver(IHubMessage message)
    {
        if (message is TEvent msg && deliveryAction is not null && (filter is null || filter.Invoke(msg)))
            await deliveryAction.Invoke(msg);
    }
}