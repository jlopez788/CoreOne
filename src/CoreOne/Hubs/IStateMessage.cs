namespace CoreOne.Hubs;

internal interface IStateMessage : IHubMessage
{
    StateKey Key { get; }
}
