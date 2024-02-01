using OneCore.Hubs;

namespace OneCore.Extensions;

public static class HubExtensions
{
    public static void Intercept<TEvent>(this IHub hub, InterceptHubMessage<TEvent> onintercept, CancellationToken token) where TEvent : IHubMessage => hub?.Intercept(onintercept, 0, token);

    public static void OnComplete<TEvent>(this HubPublish<TEvent> publish, Action<TEvent> oncomplete) => publish.OnComplete(Wrapper(oncomplete));

    public static void Subscribe<TEvent>(this IHub hub, Action? onmessage, CancellationToken token, Predicate<TEvent>? messageFilter = null) where TEvent : IHubMessage => hub?.Subscribe(Wrapper<TEvent>(p => onmessage?.Invoke()), token, messageFilter);

    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent>? onmessage, CancellationToken token, Predicate<TEvent>? messageFilter = null) where TEvent : IHubMessage => hub?.Subscribe(Wrapper(onmessage), token, messageFilter);

    private static Func<TEvent, Task> Wrapper<TEvent>(Action<TEvent>? callback) => msg =>
    {
        callback?.Invoke(msg);
        return Task.CompletedTask;
    };
}
