namespace CoreOne.Hubs;

public interface IHub : IDisposable
{
    TState GetState<TState>(string? name = null) where TState : IHubState<TState>;

    void Intercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order, CancellationToken token);

    HubPublish<TEvent> Publish<TEvent>(TEvent message);

    void Subscribe<TEvent>(Func<TEvent, Task> onmessage, CancellationToken token, Predicate<TEvent>? filter = null);

    void SubscribeState<TEvent>(string? name, Func<TEvent, Task> onstate, CancellationToken token, Predicate<TEvent>? filter = null) where TEvent : IHubState<TEvent>;
}