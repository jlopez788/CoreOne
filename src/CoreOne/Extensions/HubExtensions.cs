using CoreOne.Hubs;
using CoreOne.Reactive;

namespace CoreOne.Extensions;

public static class HubExtensions
{
    public static void Intercept<TEvent>(this IHub hub, InterceptHubMessage<TEvent> onintercept, CancellationToken token) => hub?.Intercept(onintercept, 0, token);

    public static void OnComplete<TEvent>(this HubPublish<TEvent> publish, Action<TEvent> oncomplete) => publish.OnComplete(Wrapper(oncomplete));

    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent> onmessage, CancellationToken token) => hub.Subscribe(Wrapper(onmessage), null, token);

    public static void Subscribe<TEvent>(this IHub hub, Func<TEvent, Task> onmessage, CancellationToken token) => hub.Subscribe(onmessage, null, token);

    public static void Subscribe<TEvent>(this IHub hub, Action? onmessage, Predicate<TEvent>? filter, CancellationToken token) => hub?.Subscribe(Wrapper<TEvent>(p => onmessage?.Invoke()), filter, token);

    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent>? onmessage, Predicate<TEvent>? filter, CancellationToken token) => hub?.Subscribe(Wrapper(onmessage), filter, token);

    public static void SubscribeState<TEvent>(this IHub hub, Action<TEvent> onstate, Predicate<TEvent>? filter, CancellationToken token) where TEvent : IHubState<TEvent>
    {
        hub.SubscribeState(null, Wrapper(onstate), filter, token);
    }

    public static void SubscribeState<TEvent>(this IHub hub, string name, Action<TEvent> onstate, CancellationToken token, Predicate<TEvent>? filter = null) where TEvent : IHubState<TEvent>
    {
        hub.SubscribeState(name, Wrapper(onstate), filter, token);
    }

    public static void SubscribeState<TEvent>(this IHub hub, Func<TEvent, Task> onstate, CancellationToken token, Predicate<TEvent>? filter = null) where TEvent : IHubState<TEvent>
    {
        hub.SubscribeState(null, onstate, filter, token);
    }

    public static IObservable<TEvent> ToObservable<TEvent>(this IHub hub) => new Observable.HubObserver<TEvent>(hub);

    private static Func<TEvent, Task> Wrapper<TEvent>(Action<TEvent>? callback) => msg => {
        callback?.Invoke(msg);
        return Task.CompletedTask;
    };
}