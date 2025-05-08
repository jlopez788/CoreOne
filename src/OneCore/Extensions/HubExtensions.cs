using CoreOne.Hubs;

namespace CoreOne.Extensions;


public static class HubExtensions
{
    public static void Intercept<TEvent>(this IHub hub, InterceptHubMessage<TEvent> onintercept, CancellationToken token) where TEvent : IHubMessage => hub?.Intercept(onintercept, 0, token);

    public static void OnComplete<TEvent>(this HubPublish<TEvent> publish, Action<TEvent> oncomplete) => publish.OnComplete(Wrapper(oncomplete));

    public static void Subscribe<TEvent>(this IHub hub, Action? onmessage, CancellationToken token, Predicate<TEvent>? messageFilter = null) where TEvent : IHubMessage => hub?.Subscribe(Wrapper<TEvent>(p => onmessage?.Invoke()), token, messageFilter);

    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent>? onmessage, CancellationToken token, Predicate<TEvent>? messageFilter = null) where TEvent : IHubMessage => hub?.Subscribe(Wrapper(onmessage), token, messageFilter);

    public static void SubscribeState<T>(this IHub hub, Action<T?> onmessage, SToken token, Predicate<T>? messageFilter = null) => hub?.SubscribeState(null, Wrapper(onmessage), token);

    public static void SubscribeState<T>(this IHub hub, string? name, Action<T?> onmessage, CancellationToken token, Predicate<T>? messageFilter = null) => hub?.SubscribeState(name, Wrapper(onmessage), token);

    public static void SubscribeState<T>(this IHub hub, Func<T?, Task> onstate, SToken token) => hub?.SubscribeState(null, onstate, token);

    public static IObservable<TEvent> ToObservable<TEvent>(this IHub hub) where TEvent : IHubMessage
    {
        return new Observable.HubObserver<TEvent>(hub);
    }

    [return: NotNullIfNotNull(nameof(getDefaultState))]
    public static TState? GetState<TState>(this IHub hub, Func<TState>? getDefaultState = null) => hub.GetState(null, getDefaultState);

    [return: NotNullIfNotNull(nameof(getDefaultState))]
    public static TState? GetState<TState>(this IHub hub, string? name, Func<TState>? getDefaultState = null)
    {
        return hub.TryGetState(name, out var state, getDefaultState) ? state :
            (getDefaultState is not null ? getDefaultState.Invoke() : default);
    }

    [return: NotNullIfNotNull(nameof(getDefaultState))]
    public static bool TryGetState<T>(this IHub hub, [NotNullWhen(true)] out T? state, Func<T>? getDefaultState = null) => hub.TryGetState(null, out state, getDefaultState);

    [return: NotNullIfNotNull(nameof(getDefaultState))]
    public static bool TryGetState<T>(this IHub hub, string? name, [NotNullWhen(true)] out T? state, Func<T>? getDefaultState = null)
    {
        state = default;
        if (hub.TryGetState<T>(null, out var current))
            state = current;
        else if (getDefaultState is not null)
            state = getDefaultState.Invoke();
        return state is not null;
    }

    private static Func<TEvent, Task> Wrapper<TEvent>(Action<TEvent>? callback) => msg => {
        callback?.Invoke(msg);
        return Task.CompletedTask;
    };
}
