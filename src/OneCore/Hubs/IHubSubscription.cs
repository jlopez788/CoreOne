namespace CoreOne.Hubs;

internal interface IHubSubscription
{
    ValueTask Deliver(IHubMessage message);
}
