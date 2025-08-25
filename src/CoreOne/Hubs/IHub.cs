namespace CoreOne.Hubs;

public interface IHub : IDisposable
{
#if NET9_0_OR_GREATER
    TState GetState<TState>(string? name = null) where TState : IHubState<TState>;
#else
    TState? GetState<TState>(string? name = null) where TState : IHubState<TState>;
#endif

    void Intercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order, CancellationToken token);

    HubPublish<TEvent> Publish<TEvent>(TEvent message);

    void Subscribe<TEvent>(Func<TEvent, Task> onmessage, CancellationToken token, Predicate<TEvent>? filter = null);

    void SubscribeState<TEvent>(string? name, Func<TEvent, Task> onstate, CancellationToken token, Predicate<TEvent>? filter = null) where TEvent : IHubState<TEvent>;
}