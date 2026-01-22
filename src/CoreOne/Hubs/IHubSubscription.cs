namespace CoreOne.Hubs;

internal interface IHubSubscription
{
    ValueTask Deliver(object message);
}