namespace CoreOne.Hubs;

internal class MessageSubscription<TEvent>(Func<TEvent, Task> deliveryAction, Predicate<TEvent>? filter) : IHubSubscription
{
    public async ValueTask Deliver(IHubMessage message)
    {
        if (message is TEvent msg && deliveryAction is not null && CanDeliver(msg))
            await deliveryAction.Invoke(msg);
    }

    protected virtual bool CanDeliver(TEvent message) => filter is null || filter.Invoke(message);
}