# CoreOne Services, Lifecycle, Concurrency & Collections

This reference covers service base classes, cancellation/disposal tokens, async queues, debouncing, synchronization primitives, and specialized collections in `CoreOne`.

---

## 1. Services & Dependency Injection (`CoreOne.Services`)

### `BaseService : IDisposable, IAsyncDisposable`
Standard base class for application and domain services:
- **Constructor**: `public BaseService(IServiceProvider? services)`
  - Creates a dedicated `protected AToken Token { get; init; }` for the service's lifetime.
  - Stores `protected IServiceProvider? ServiceProvider { get; init; }`.
  - Runs `ServiceInitializer.Initialize(this, services)` which automatically populates properties decorated with `[OInject]`.
- **Property Injection**:
  - `[OInject(Optional = true)] protected ILogger<BaseService>? Logger { get; init; }` is built in.
  - Derived classes can declare `[OInject] protected IMyDependency MyDep { get; init; } = default!;` or resolve via `protected TService Get<TService>()`.
- **Disposal**:
  - **Prefer `await service.DisposeAsync()`** (`Dispose()` is marked `[Obsolete("Prefer use DisposeAsync method")]`).
  - `DisposeAsync()` automatically disposes `Token` (cancelling all `IHub` and `IObservable` subscriptions and running registered cleanup tasks), then calls `protected virtual void Dispose(bool disposing)` and `protected virtual ValueTask DisposeAsync(bool disposing)`.
- **Structured Result Logging**:
  - `protected void LogResult<TResult>(TResult? result, string msg, [CallerMemberName] string? name = null) where TResult : IResult`
  - Logs a warning on `ResultType.Fail` and an error (`LogEntryX`) on `ResultType.Exception` (including `StatusCode` if present).

---

## 2. Lifecycle & Cancellation Tokens (`CoreOne`)

### `AToken : IAsyncDisposable` (Async-First Lifecycle Token)
Manages a `CancellationTokenSource` plus an asynchronous cleanup list (`ImmutableList<Func<Task>>`):
- **Creation**: `var token = AToken.Create();` (or `AToken.Empty`).
- **Implicit Conversions**:
  - `AToken -> CancellationToken` (pass directly to any method accepting `CancellationToken`).
  - `AToken -> SToken`
- **Registering Cleanup Work (executed on `await token.DisposeAsync()`)**:
  - `token.Register(IDisposable disposable)`
  - `token.Register(Action callback)`
  - `token.Register(Func<Task> callback)`
  - `token.RegisterAsync(IAsyncDisposable asyncDisposable)`

### `SToken : IComponent, IDisposable` (Sync-First Cancellation Token)
Wrapper around `CancellationTokenSource` implementing `IDisposable` and `IComponent`:
- **Creation**:
  - `SToken.Create()`
  - `SToken.CreateLinkedTokens(params CancellationToken[] tokens)`
  - `SToken.Empty`
- **Implicit Conversion**: `SToken -> CancellationToken`
- **Methods**:
  - `token.Cancel()` — Cancels the underlying `CancellationTokenSource`.
  - `token.Register(IDisposable)` / `token.Register(Action)` — Registers immediate cancellation callbacks on the underlying `CancellationToken`.
  - `token.Dispose()` — Cancels and disposes the token source (and triggers `Disposed` event).

### `Disposable : IDisposable` & `Subscription : IDisposable`
- **`Disposable`**: Base class with `public bool IsDisposed { get; }`, `public static readonly Disposable Empty`, and `protected virtual void OnDispose()` template method.
- **`Subscription(Action callback)`**: Thread-safe `IDisposable` that invokes `callback` at most once (via `Interlocked.Exchange`) when disposed.

---

## 3. Concurrency, Queuing & Debouncing

### `SafeLock` (`CoreOne.Threading`)
Cross-target lock primitive (uses `System.Threading.Lock` on .NET 9+ and `Monitor` on older targets):
```csharp
private readonly SafeLock Sync = new();

public void ThreadSafeOperation()
{
    using (Sync.EnterScope())
    {
        // Critical section
    }
}
```

### `AsyncTaskQueue(int concurrency = 1)` (`CoreOne`)
Queues synchronous or asynchronous work items with bounded concurrency (defaults to `1` for serial execution):
```csharp
var queue = new AsyncTaskQueue(concurrency: 1);

await queue.Enqueue(() => DoSyncWork());
await queue.Enqueue(async () => await DoAsyncWork(), cancellationToken);
int value = await queue.Enqueue(async () => await ComputeAsync(), cancellationToken);

// Wait for all currently queued tasks to complete
await queue.WhenAll();
```

### `Debounce` vs `DebounceTrigger` (`CoreOne.Services`)
| Type | Behavior | Usage |
|---|---|---|
| `Debounce` / `Debounce<TModel>` | **Trailing-edge debounce**: Every `Invoke(...)` resets the timer. The callback only runs after `delay` elapses with no new triggers (unless `skipDelay: true` or `delay == TimeSpan.Zero`). | Search-as-you-type, window resize, auto-save after typing stops. |
| `DebounceTrigger` / `DebounceTrigger<TModel>` | **Leading-first + trailing debounce**: The **very first** `Invoke(...)` executes **immediately**; subsequent calls within the window cancel any pending/in-progress token and debounce by `delay`. | Button clicks or refresh triggers where the first action should feel instant, but rapid repeats are throttled/debounced. |

```csharp
// Trailing debounce (300ms)
using var searchDebounce = new Debounce<string>(query => RunSearch(query), delayMS: 300);
searchDebounce.Invoke("core");
searchDebounce.Invoke("coreone"); // Only "coreone" fires after 300ms

// Leading-first + debounce (500ms) with cancellation support
using var saveTrigger = new DebounceTrigger<Document>(
    async (doc, ct) => await SaveAsync(doc, ct),
    delayMS: 500
);
saveTrigger.Invoke(doc1); // Fires immediately!
saveTrigger.Invoke(doc2); // Cancels doc1's token and waits 500ms before saving doc2
```

---

## 4. Specialized Collections (`CoreOne.Collections`)

### `Data<TKey, TValue> : Dictionary<TKey, TValue>`
A safe dictionary that never throws `KeyNotFoundException` on indexer reads:
- **`this[TKey key]`**:
  - Getter returns the value if found; otherwise falls back to `DefaultKey` (if set and present), or `default(TValue)`.
  - Setter safely adds or updates the key (no-op if `key is null`).
- **`Get(TKey key, Func<TValue> getDefault)`**: Gets existing value or computes, stores, and returns `getDefault()`.
- **`Set(TKey key, TValue? value)`**: Sets the value if non-null, or removes the key if `value is null`.

### `DataList<TKey, TValue>` & `DataHashSet<TKey, TValue>`
Multi-value dictionaries (`DataCollection<K, V, List<V>>` and `DataCollection<K, V, HashSet<V>>`):
```csharp
var groups = new DataList<string, User>();
groups.Add("Admins", alice);
groups.Add("Admins", bob);
groups.RemoveAll((role, user) => !user.IsActive);
```

### `ConcurrentSet<T> : ICollection<T>` (`CoreOne.Collections.Concurrent`)
Thread-safe set backed by `HashSet<T>` and `SafeLock`:
- `bool Add(T item)`, `void AddRange(IEnumerable<T> items)`, `bool Remove(T item)`, `bool Contains(T item)`
- Snapshot iteration (safe against concurrent modification):
  - `set.Each(item => ...)`
  - `await set.EachAsync(async item => ...)`
  - `IReadOnlyCollection<T> matches = set.Filter(item => item.IsValid)`
  - `IReadOnlyCollection<T> snapshot = set.ToList()`
