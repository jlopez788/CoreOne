namespace CoreOne.Hubs;

public interface IGlobalHubMessage : IHubMessage
{
    bool IsGlobal { get; }
}
