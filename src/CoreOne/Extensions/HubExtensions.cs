using CoreOne.Hubs;
using CoreOne.Reactive;

namespace CoreOne.Extensions;

public static class HubExtensions
{
    public static void Intercept<TEvent>(this IHub hub, InterceptHubMessage<TEvent> onintercept, CancellationToken cancellationToken) => hub?.Intercept(onintercept, 0, cancellationToken);

    public static void OnComplete<TEvent>(this HubPublish<TEvent> publish, Action<TEvent> oncomplete) => publish.OnComplete(Wrapper(oncomplete));

    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent> onmessage, CancellationToken cancellationToken) => hub.Subscribe(Wrapper(onmessage), null, cancellationToken);

    public static void Subscribe<TEvent>(this IHub hub, Func<TEvent, Task> onmessage, CancellationToken cancellationToken) => hub.Subscribe(onmessage, null, cancellationToken);

    public static void Subscribe<TEvent>(this IHub hub, Action? onmessage, Predicate<TEvent>? filter, CancellationToken cancellationToken) => hub?.Subscribe(Wrapper<TEvent>(p => onmessage?.Invoke()), filter, cancellationToken);

    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent>? onmessage, Predicate<TEvent>? filter, CancellationToken cancellationToken) => hub?.Subscribe(Wrapper(onmessage), filter, cancellationToken);

    public static void SubscribeState<TEvent>(this IHub hub, Action<TEvent> onstate, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>
    {
        hub.SubscribeState(null, Wrapper(onstate), null, cancellationToken);
    }

    public static void SubscribeState<TEvent>(this IHub hub, Action<TEvent> onstate, Predicate<TEvent>? filter, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>
    {
        hub.SubscribeState(null, Wrapper(onstate), filter, cancellationToken);
    }

    public static void SubscribeState<TEvent>(this IHub hub, string name, Action<TEvent> onstate, Predicate<TEvent>? filter, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>
    {
        hub.SubscribeState(name, Wrapper(onstate), filter, cancellationToken);
    }

    public static void SubscribeState<TEvent>(this IHub hub, Func<TEvent, Task> onstate, Predicate<TEvent>? filter , CancellationToken cancellationToken) where TEvent : IHubState<TEvent>
    {
        hub.SubscribeState(null, onstate, filter, cancellationToken);
    }

    public static IObservable<TEvent> ToObservable<TEvent>(this IHub hub) => new Observable.HubObserver<TEvent>(hub);

    private static Func<TEvent, Task> Wrapper<TEvent>(Action<TEvent>? callback) => msg => {
        callback?.Invoke(msg);
        return Task.CompletedTask;
    };
}