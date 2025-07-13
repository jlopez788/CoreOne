namespace CoreOne.Hubs;

public interface IHubState<TState> : IHubMessage
{
    static abstract TState Default { get; }
    string? Name { get; }
}