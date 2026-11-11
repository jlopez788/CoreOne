# CoreOne Reactive Streams (`CoreOne.Reactive`)

`CoreOne.Reactive` provides lightweight, thread-safe implementations of `IObservable<T>` and `IObserver<T>` along with LINQ-style stream operators (`Select`, `Where`, `Distinct`, `Throttle`, `FromEvent`).

---

## 1. Subjects & Observers

### `ObserverBase<T> : Disposable, IObserver<T>`
Abstract base class for custom observers:
- Implements `OnNext(T value)`, `OnError(Exception exception)`, and `OnCompleted()`.
- Tracks `IsFinalized` (set to `true` on `OnCompleted` or `OnError`) and `protected Exception? Exception`. Once finalized, subsequent `OnNext` calls are ignored.
- Disposing an `ObserverBase<T>` automatically calls `OnCompleted()`.
- Derived classes implement `protected abstract void OnNextCore(T value)` and optionally override `OnCompletedCore()` and `OnErrorCore(Exception)`.

### `Subject<T> : ObserverBase<T>, IObserver<T>, IObservable<T>`
Thread-safe hot observable multicast subject backed by `ImmutableList<IObserver<T>>` and `SafeLock`:
- **Properties**: `bool HasObservers`
- **Publishing**: Call `subject.OnNext(value)`, `subject.OnError(ex)`, or `subject.OnCompleted()`.
  - `OnCompleted()` and `OnError(ex)` snapshot the current observers, clear the `Observers` list, and notify all observers.
- **Subscribing**:
  - `IDisposable Subscribe(IObserver<T> observer)` returns a thread-safe `Subscription` disposable that removes the observer when disposed.
  - Or use the `Observable.Subscribe(...)` extension methods with a `CancellationToken`.

### `BehaviorSubject<T>(T? value = default) : Subject<T>` (sealed)
Stateful subject that remembers its latest value and **emits it immediately to new subscribers** (when `Value is not null`):
- **`T? Value`**: Gets the current value (or rethrows `Exception` if `OnError` was called).
- **`bool TryGetValue([NotNullWhen(true)] out T? value)`**:
  - Returns `false` if disposed or if `CurrentValue is null`.
  - Throws `Exception` if the subject faulted via `OnError`.
  - Returns `true` and outputs `value` when a non-null value is present.
- **Disposal**: Resets `CurrentValue = default` on `Dispose()`.

```csharp
var currentUser = new BehaviorSubject<User?>(initialUser);

// Immediately emits initialUser (if non-null) and all subsequent OnNext values
currentUser.Subscribe(user => UpdateHeader(user), Token);

// Push a new state
currentUser.OnNext(updatedUser);

// Safe synchronous read
if (currentUser.TryGetValue(out var activeUser))
{
    Console.WriteLine(activeUser.Name);
}
```

### Non-Generic `Subject` (Void Signal / Trigger)
A lightweight notification subject for parameterless signals:
- `bool HasObservers { get; }`
- `void Subscribe(Action callback, CancellationToken cancellationToken)`
- `void Subscribe(Func<Task> callback, CancellationToken cancellationToken)`
- `void Publish()` — Invokes all non-cancelled subscribers.

```csharp
var refreshSignal = new Subject();
refreshSignal.Subscribe(async () => await ReloadDataAsync(), Token);
refreshSignal.Publish();
```

### `Observer.Create<T>`
Factory for creating an inline `IObserver<T>`:
```csharp
IObserver<int> observer = Observer.Create<int>(
    onNext: val => Console.WriteLine(val),
    onError: ex => Console.Error.WriteLine(ex),
    onCompleted: () => Console.WriteLine("Done")
);
```

---

## 2. `Observable` Operators & Extensions (`CoreOne.Reactive.Observable`)

### Creating Observables from .NET Events: `Observable.FromEvent<TEventArgs>`
Uses reflection to attach an event handler to `target` and removes the handler when disposed:
```csharp
IObservable<PropertyChangedEventArgs> propChanged =
    Observable.FromEvent<PropertyChangedEventArgs>(viewModel, nameof(INotifyPropertyChanged.PropertyChanged));
```

### Stream Operators
All operators return a new `IObservable<T>` (implemented as a specialized `Subject<T>` that manages an internal `SToken` linked to the upstream subscription):

| Operator | Signature | Behavior |
|---|---|---|
| `Where` | `IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate)` | Emits only items matching `predicate`. |
| `Select` (sync) | `IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector)` | Transforms each emitted item synchronously. |
| `Select` (async) | `IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> selector)` | Transforms each emitted item asynchronously; forwards task exceptions to `OnError`. |
| `Distinct` | `IObservable<TSource> Distinct<TSource>(this IObservable<TSource> source, IEqualityComparer<TSource>? comparer = null)`<br>`IObservable<TSource> Distinct<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer = null)` | Suppresses consecutive duplicate values (`DistinctUntilChanged` semantics using `BackingField<TKey>`). Only emits when the key changes from the previous emission! |
| `Throttle` | `IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, int durationMs)`<br>`IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, TimeSpan duration)` | Debounces rapid emissions using `Debounce<TSource>`, emitting only after `duration` has elapsed without another item. |

### Subscribing with `CancellationToken`
Instead of manually tracking `IDisposable` handles, pass a `CancellationToken` (`AToken` or `SToken`):
```csharp
// Sync callback
source.Subscribe(item => Process(item), Token);

// Sync callback + onComplete
source.Subscribe(item => Process(item), oncomplete: () => Cleanup(), Token);

// Async callback (executed via Task.Factory.StartNew)
source.Subscribe(async item => await ProcessAsync(item), Token);

// Async callback + onComplete
source.Subscribe(async item => await ProcessAsync(item), oncomplete: () => Cleanup(), Token);

// IObserver<T> + CancellationToken
source.Subscribe(observer, Token);

// Parameterless callback for IObservable<EventArgs>
eventSource.Subscribe(() => HandleEvent(), Token);
```

---

## 3. Nuances & Best Practices

1. **`Distinct` is `DistinctUntilChanged`**: Internally, `DistinctObserver` uses `BackingField<TKey>.UpdateValue(key)`, meaning it compares each new emission against the **immediately preceding emission**, not an unbounded history set. Note that if `comparer` is omitted, it defaults to `ReferenceEqualityComparer<TKey>.Default`—for value/record structural comparisons where reference equality isn't desired, pass `EqualityComparer<TKey>.Default` or `StringComparer.OrdinalIgnoreCase`.
2. **`BehaviorSubject<T>` null handling**: `BehaviorSubject<T>` only emits on initial `Subscribe` if `Value is not null`.
3. **Token Lifecycle**: Always pass the owning component/service's `Token` (`AToken` or `SToken`) to `.Subscribe(..., Token)` so subscriptions are automatically disposed when the owner is disposed.
