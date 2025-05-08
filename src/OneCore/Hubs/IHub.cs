namespace CoreOne.Hubs;

public interface IHub : IDisposable
{
    void Intercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order, CancellationToken token) where TEvent : IHubMessage;

    HubPublish<TEvent> Publish<TEvent>(TEvent message) where TEvent : IHubMessage;

    void PublishState<T>(T? model, string? name = null);

    void Subscribe<TEvent>(Func<TEvent, Task> onmessage, CancellationToken token, Predicate<TEvent>? messageFilter = null) where TEvent : IHubMessage;

    void SubscribeState<T>(string? name, Func<T?, Task> onstate, CancellationToken token);

    bool TryGetState<TState>(string? name, [NotNullWhen(true)] out TState? state);
}