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

    void Subscribe<TEvent>(Func<TEvent, Task> onmessage, Predicate<TEvent>? filter, CancellationToken token);

    void SubscribeState<TEvent>(string? name, Func<TEvent, Task> onstate, Predicate<TEvent>? filter, CancellationToken token) where TEvent : IHubState<TEvent>;
}