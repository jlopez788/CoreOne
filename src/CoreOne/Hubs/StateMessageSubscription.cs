namespace CoreOne.Hubs;

internal class StateMessageSubscription<TEvent>(string? name, Func<TEvent, Task> deliveryAction, Predicate<TEvent>? filter) : MessageSubscription<TEvent>(deliveryAction, filter), IHubSubscription where TEvent : IHubState<TEvent>
{
    protected override bool CanDeliver(TEvent message) => ((name is null && message.Name is null) || name.Matches(message.Name)) && base.CanDeliver(message);
}