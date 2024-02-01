namespace OneCore.Hubs;

internal interface IHubSubscription
{
    ValueTask Deliver(IHubMessage message);
}
