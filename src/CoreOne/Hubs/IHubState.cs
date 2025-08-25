namespace CoreOne.Hubs;

public interface IHubState<TState> : IHubMessage
{
#if NET9_0_OR_GREATER
    static abstract TState Default { get; }
#endif

    string? Name { get; }
}