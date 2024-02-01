namespace OneCore.Hubs;

public interface IGlobalHubMessage : IHubMessage
{
    bool IsGlobal { get; }
}
