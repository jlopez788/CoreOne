# CoreOne Pub/Sub & State Hub (`CoreOne.Hubs` & `CoreOne.Extensions`)

`IHub` and `Hub` provide an in-memory, asynchronous publish/subscribe event bus with message inheritance matching, ordered interceptors, named/typed state caching (`IHubState<T>`), and global cross-hub broadcasting (`IGlobalHubMessage`).

---

## 1. Core Interfaces (`CoreOne.Hubs`)

### `IHub : IDisposable`
```csharp
public interface IHub : IDisposable
{
    // Retrieves cached state (or TState.Default on .NET 9+ if not yet published)
    TState GetState<TState>(string? name = null) where TState : IHubState<TState>;

    // Registers an ordered interceptor that can allow or cancel message delivery
    void Intercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order, CancellationToken token);

    // Enqueues a message for async delivery to all matching subscribers
    HubPublish<TEvent> Publish<TEvent>(TEvent message);

    // Subscribes to messages of type TEvent (including derived types / implementations)
    void Subscribe<TEvent>(Func<TEvent, Task> onmessage, Predicate<TEvent>? filter, CancellationToken token);

    // Subscribes to stateful messages and immediately delivers current state if present
    void SubscribeState<TEvent>(string? name, Func<TEvent, Task> onstate, Predicate<TEvent>? filter, CancellationToken token)
        where TEvent : IHubState<TEvent>;
}
```

### Message Marker Interfaces
| Interface | Members | Behavior |
|---|---|---|
| `IHubMessage` | *(marker)* | Base contract for hub messages. Note: `IHub.Subscribe<TEvent>` and `Publish<TEvent>` work with **any** type `TEvent`, not just `IHubMessage`. |
| `IGlobalHubMessage : IHubMessage` | `bool IsGlobal { get; }` | When `IsGlobal == true`, calling `hub.Publish(msg)` on **any** `Hub` instance broadcasts the message to **all** active `Hub` instances tracked by `Hub.Global`. |
| `IHubState<TState> : IHubMessage` | `static abstract TState Default { get; }` (.NET 9+)<br>`string? Name { get; }` | Marks a message as stateful. Publishing it caches the latest instance in the hub keyed by `(typeof(TState), state.Name)`. |

### Built-in Messages & Delegates
- **`ExceptionMessage(Exception Exception, string Message) : IHubMessage`**: Automatically published to the hub if an interceptor or subscriber throws an unhandled exception during `Publish`.
- **`InterceptHubMessage<TEvent>`**: `public delegate Task<ResultType> InterceptHubMessage<TEvent>(TEvent message);`

---

## 2. `Hub` Implementation Behavior

- **DI Registration**: `Hub` is decorated with `[Service]` and implements `IHub`.
- **Global Singleton (`Hub.Global`)**: A static `Hub` instance exists at `Hub.Global`. Every `new Hub()` registers itself in a static `Instances` list and removes itself on `Dispose()`. Disposing `Hub.Global` disposes all tracked `Hub` instances.
- **Sequential Async Queue (`AsyncTaskQueue`)**: Each `Hub` processes `Publish` calls sequentially via an internal `AsyncTaskQueue(concurrency: 1)`. `Publish` returns immediately with a `HubPublish<TEvent>` wrapper around the queued task.
- **Polymorphic Delivery**: Publishing a concrete `TEvent` delivers to subscribers and interceptors registered for:
  - The exact type `TEvent`
  - Any base class of `TEvent`
  - Any interface implemented by `TEvent`
- **CancellationToken Unsubscription**: `Subscribe`, `SubscribeState`, and `Intercept` do **not** return `IDisposable`. Instead, they accept a `CancellationToken` (such as `AToken` or `SToken`) and automatically unregister the subscription when the token is cancelled.

---

## 3. Publishing & Completion (`Publish` & `HubPublish<TEvent>`)

`Publish` queues delivery and returns `HubPublish<TEvent>`, which allows attaching a completion callback that runs after all interceptors and subscribers have finished processing the message:

```csharp
// Fire-and-forget publish (queued on Hub's AsyncTaskQueue)
hub.Publish(new OrderCreatedEvent(orderId));

// Run a callback once all subscribers have processed the message
hub.Publish(new OrderCreatedEvent(orderId))
   .OnComplete(async evt => await Logger.LogAsync($"Processed {evt.OrderId}"));

// Sync OnComplete extension (from HubExtensions)
hub.Publish(new OrderCreatedEvent(orderId))
   .OnComplete(evt => Console.WriteLine($"Done: {evt.OrderId}"));
```

---

## 4. Subscribing (`Subscribe` & `HubExtensions`)

Use `CoreOne.Extensions.HubExtensions` for ergonomic sync (`Action<TEvent>`, `Action`) and async (`Func<TEvent, Task>`) subscriptions:

```csharp
// 1. Sync subscriber (no filter)
hub.Subscribe<OrderCreatedEvent>(evt => HandleOrder(evt), Token);

// 2. Async subscriber (no filter)
hub.Subscribe<OrderCreatedEvent>(async evt => await HandleOrderAsync(evt), Token);

// 3. Subscriber with predicate filter
hub.Subscribe<OrderCreatedEvent>(
    onmessage: evt => HandlePriorityOrder(evt),
    filter: evt => evt.Total > 1000m,
    cancellationToken: Token
);

// 4. Parameterless action with filter
hub.Subscribe<RefreshDashboardEvent>(
    onmessage: () => ReloadUi(),
    filter: evt => evt.ForceRefresh,
    cancellationToken: Token
);
```

---

## 5. Stateful Messages (`IHubState<TState>`)

When a type implements `IHubState<TState>`:
1. Publishing an instance stores it in the hub's state dictionary under `StateKey.Create<TState>(message.Name)`.
2. Calling `hub.GetState<TState>(name)` retrieves the last published state (or `TState.Default` on .NET 9+ if none was published).
3. Calling `hub.SubscribeState<TState>(...)` (or even `hub.Subscribe<TState>(...)`, which automatically redirects to `SubscribeState` when `TState` implements `IHubState<>`) registers the subscriber **and immediately invokes the callback with the current cached state if one exists**.

```csharp
public record ThemeState(string ThemeName, string? Name = null) : IHubState<ThemeState>
{
    public static ThemeState Default => new("Light");
}

// Publish state update (caches under Name = null)
hub.Publish(new ThemeState("Dark"));

// Read current state synchronously
ThemeState current = hub.GetState<ThemeState>(); // ThemeState("Dark")

// Subscribe to state (immediately receives "Dark", plus any future updates)
hub.SubscribeState<ThemeState>(state => ApplyTheme(state.ThemeName), Token);

// Named state instances (e.g. multiple panels/regions)
hub.Publish(new ThemeState("HighContrast", Name: "Sidebar"));
ThemeState sidebarTheme = hub.GetState<ThemeState>("Sidebar");
hub.SubscribeState<ThemeState>("Sidebar", state => ApplySidebarTheme(state), filter: null, Token);
```

---

## 6. Message Interception (`Intercept`)

Interceptors run **in ascending `order`** before any subscriber receives a published message:
- Each interceptor returns `Task<ResultType>`.
- Interceptors are evaluated sequentially via `AggregateResultAsync`.
- If **all** matching interceptors return `ResultType.Success`, the message is delivered to subscribers.
- If **any** interceptor returns a non-success `ResultType` (e.g., `ResultType.Fail` or `ResultType.Cancelled`), delivery is **aborted** and subscribers are not invoked.

```csharp
// Default order (0) via HubExtensions
hub.Intercept<DeleteUserCommand>(async cmd =>
{
    bool allowed = await authService.CanDeleteAsync(cmd.UserId);
    return allowed ? ResultType.Success : ResultType.Fail;
}, Token);

// Explicit execution order (lower runs first)
hub.Intercept<DeleteUserCommand>(
    onintercept: async cmd => { await auditLog.RecordAsync(cmd); return ResultType.Success; },
    order: -10,
    token: Token
);
```

---

## 7. Bridging `IHub` to `IObservable<T>`

Use `hub.ToObservable<TEvent>()` (in `HubExtensions`) to turn any hub event stream into an `IObservable<TEvent>` so you can apply `CoreOne.Reactive` operators (`Where`, `Select`, `Distinct`, `Throttle`):

```csharp
hub.ToObservable<SearchQueryChangedMessage>()
   .Select(msg => msg.Query.Trim())
   .Distinct()
   .Throttle(250)
   .Subscribe(async query => await ExecuteSearchAsync(query), Token);
```
