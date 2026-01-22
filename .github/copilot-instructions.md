# CoreOne Copilot Instructions

## Architecture & SOLID Principles

### Single Responsibility Principle (SRP)
- Each class should have a single, well-defined purpose
- **Examples**: [Hub.cs](../src/CoreOne/Hubs/Hub.cs) handles messaging/pub-sub, [Subject.cs](../src/CoreOne/Reactive/Subject.cs) manages observer subscriptions
- Separate concerns: Use extension methods in dedicated files ([StringExtensions.cs](../src/CoreOne/Extensions/StringExtensions.cs), [EnumerableExtensions.cs](../src/CoreOne/Extensions/EnumerableExtensions.cs))
- Partial classes for functionality grouping: [Utility.cs](../src/CoreOne/Utility.cs), [Utility.json.cs](../src/CoreOne/Utility.json.cs), [Utility.hash.cs](../src/CoreOne/Utility.hash.cs)

### Open/Closed Principle (OCP)
- Prefer extension methods for adding functionality without modifying existing types
- Use virtual methods to enable extension: `protected virtual void OnDispose()` in [Disposable.cs](../src/CoreOne/Disposable.cs)
- Implement template methods: Base class defines algorithm structure, derived classes override specific steps
- Use abstract base classes when common behavior is needed: [ObserverBase.cs](../src/CoreOne/Reactive/ObserverBase.cs)

### Liskov Substitution Principle (LSP)
- Interfaces must be fully substitutable: [IResult.cs](../src/CoreOne/Results/IResult.cs), `IResult<T>`, `IResult<TModel, TStatus>`
- Derived classes honor base contracts: [Subject.cs](../src/CoreOne/Reactive/Subject.cs) properly implements `IObservable<T>` and `IObserver<T>`
- Don't throw `NotImplementedException` in production code

### Interface Segregation Principle (ISP)
- Keep interfaces small and focused
- **Examples**: `IHub`, `IHubMessage`, `IObservable<T>`, `IObserver<T>`
- Clients depend only on methods they use
- Compose larger contracts from smaller ones: `IResult<TModel, TStatus> : IResult<TModel>, IStatusResult<TStatus>`

### Dependency Inversion Principle (DIP)
- Depend on abstractions (interfaces), not concrete implementations
- Use constructor injection: `public BaseService(IServiceProvider? services)`
- Primary constructors for DI: `public class AsyncTaskQueue(int concurrency = 1)`
- Property injection via attributes: `[Service(Optional = true)] protected ILogger<BaseService>? Logger { get; init; }`
- Extension methods for service resolution: [ServiceProviderExtensions.cs](../src/CoreOne/Extensions/ServiceProviderExtensions.cs)

## Naming Conventions

### Casing Rules (PascalCase Everywhere)
```csharp
// ✅ Correct - All members use PascalCase
public class MyClass
{
    private readonly SafeLock Sync = new();  // Private fields: PascalCase
    protected ImmutableList<IObserver<T>> Observers { get; set; }  // Properties: PascalCase
    public bool HasObservers => Observers != null;  // Public properties: PascalCase
    
    public void ProcessQueue() { }  // Methods: PascalCase
    private async ValueTask ProcessQueueAsync() { }  // Private methods: PascalCase
}

// ❌ Incorrect - No underscores, no camelCase
private readonly SafeLock _sync;  // Wrong: underscore prefix
private readonly SafeLock sync;   // Wrong: camelCase
private bool hasObservers;        // Wrong: camelCase
```

### Interface Naming
- Prefix with "I": `IResult`, `IHub`, `IObservable<T>`, `IDisposable`
- Generic type parameters use "T" prefix: `T`, `TModel`, `TStatus`, `TService`

### Implementation Suffixes
- Use "Impl" for internal implementations: `MessageImpl`, `ChildImpl` (in tests)
- Use descriptive names for public classes: `Subject<T>`, `BehaviorSubject<T>`, not `SubjectImpl<T>`

### File Organization
- One public type per file, file name matches type name
- Partial classes for feature segregation: `Utility.cs`, `Utility.json.cs`, `Utility.hash.cs`
- Extension methods in `Extensions/` folder: `{Type}Extensions.cs` pattern

## Code Style & Modern C# Features

### Primary Constructors (Preferred)
```csharp
// ✅ Preferred - Primary constructor
public class AsyncTaskQueue(int concurrency = 1)
{
    private readonly SemaphoreSlim Lock = new(concurrency);
}

// ✅ Also valid - Traditional constructor when complex logic needed
public BaseService(IServiceProvider? services)
{
    Token = AToken.Create();
    ServiceProvider = services;
    ServiceInitializer.Initialize(this, services);
}
```

### Expression-Bodied Members (Preferred)
```csharp
// ✅ Preferred - Expression bodies for simple members
public bool Success => ResultType == ResultType.Success;
public bool HasObservers => Observers != null && !Observers.IsEmpty;
protected virtual void OnSubscribe(IObserver<T> observer) { }

// ✅ Traditional syntax for complex logic
public IDisposable Subscribe(IObserver<T> observer)
{
    if (observer == null)
        return Empty;
    
    using (Sync.EnterScope())
    {
        Observers = Observers.Add(observer);
        OnSubscribe(observer);
    }
    return new Subscription(() => { /* ... */ });
}
```

### Collection Expressions
```csharp
// ✅ Preferred - Collection expressions (C# 12+)
Observers = [];
var os = [.. Observers];
Items = [.. properties.Select(p => p.GetValue(null) as T).ExcludeNulls()];

// ❌ Avoid - Old syntax
Observers = new List<IObserver<T>>();
var os = Observers.ToArray();
```

### Pattern Matching & Switch Expressions
```csharp
// ✅ Preferred - Pattern matching
public override bool Equals(object? obj)
{
    return obj switch {
        string str => string.Compare(Code, str, true) == 0,
        LookupType<T> lookup => string.Compare(Code, lookup.Code, true) == 0,
        _ => false,
    };
}

// ✅ Null checking patterns
if (observer is null) return;
if (observer is not null) { /* ... */ }
var result = value ?? defaultValue;
var result = value?.Property;
```

### Async/Await
- Use `async`/`await` pervasively
- Return `Task`, `Task<T>`, or `ValueTask` for async operations
- Suffix async methods with "Async": `ProcessQueueAsync()`, `DisposeAsync()`
- Use `ValueTask` for frequently-called, potentially synchronous operations
- Configure awaits when appropriate: `await task.ConfigureAwait(false)`

### Null Safety
```csharp
// ✅ Enable nullable reference types (project-wide)
#nullable enable

// ✅ Use nullable annotations
public string Code { get; set; }        // Non-nullable
public string? Description { get; set; } // Nullable

// ✅ Null-forgiving operator when you know value is not null
return instance!;
```

### Global Usings
- Centralize common usings in [GUsings.cs](../src/CoreOne/GUsings.cs)
- Use `global using` for project-wide namespaces
- No need to repeat common usings in every file

## Design Patterns

### Disposable Pattern
```csharp
// ✅ Base class pattern - inherit from Disposable
public class MyClass : Disposable
{
    protected override void OnDispose()
    {
        // Cleanup logic here
    }
}

// ✅ Async disposal
public class BaseService : IDisposable, IAsyncDisposable
{
    public async ValueTask DisposeAsync()
    {
        await Token.DisposeAsync();
        Dispose(true);
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual ValueTask DisposeAsync(bool disposing) => ValueTask.CompletedTask;
}
```

### Observer Pattern
- Implement `IObservable<T>` for observable sources
- Implement `IObserver<T>` for observers
- Use [Subject.cs](../src/CoreOne/Reactive/Subject.cs) for hot observables
- Use [BehaviorSubject.cs](../src/CoreOne/Reactive/BehaviorSubject.cs) for state that needs current value

### Singleton/Static Instances
```csharp
// ✅ Preferred - Static readonly field
public static readonly Disposable Empty = new EmptyDisposable();

// ✅ Factory methods
public static AToken Create() => new AToken();
```

### Template Method Pattern
```csharp
// ✅ Base class defines algorithm, derived classes customize
protected override void OnNextCore(T value)
{
    var os = default(IObserver<T>[]);
    using (Sync.EnterScope())
        os = [.. Observers];
    
    os.Each(p => p.OnNext(value));
}
```

## Testing Guidelines

### Unit Testing with NUnit & Moq
```csharp
// ✅ Prefer using Moq to verify method invocation
var callback = new Mock<Action<IHubMessage<IMessage>>>();
callback.Setup(x => x(It.IsAny<IHubMessage<IMessage>>()))
        .Callback(() => tcs.SetResult());

hub.Subscribe(callback.Object);

await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
callback.Verify(x => x(It.IsAny<IHubMessage<IMessage>>()), Times.Once);

// ✅ Mock types must be public for Moq proxy generation
public interface IMessage { }  // Not private
public class MessageImpl : IMessage { }  // Not private

// ✅ Helper classes for test infrastructure
public static class Observer
{
    public static IObserver<T> Create<T>(Action<T> onNext) => 
        new AnonymousObserver<T>(onNext, ex => { }, () => { });
}
```

### Test Organization
- One test class per production class: `HubTests.cs` for [Hub.cs](../src/CoreOne/Hubs/Hub.cs)
- Descriptive test names: `Subscribe_InvokesCallbackOnPublish()`, `Throttle_LimitsEmissions()`
- Use `[Test]` attribute for test methods
- Use `[SetUp]` and `[TearDown]` for common initialization/cleanup

### Async Test Patterns
```csharp
// ✅ Use TaskCompletionSource for synchronization
var tcs = new TaskCompletionSource();
await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));

// ✅ Proper cleanup
[TearDown]
public void TearDown()
{
    hub?.Dispose();
}
```

## Documentation

### XML Documentation
```csharp
/// <summary>
/// Notifies the provider that an observer is to receive notifications.
/// </summary>
/// <param name="observer">The observer to subscribe.</param>
/// <returns>A disposable that can be used to unsubscribe.</returns>
public IDisposable Subscribe(IObserver<T> observer)
{
    // Implementation
}
```

### Comments
- Use XML docs for public APIs
- Use inline comments sparingly, prefer self-documenting code
- Explain "why" not "what" when comments are needed

## Additional Guidelines

### Error Handling
- Use `IResult<T>` pattern for operation results
- Distinguish between `ResultType.Success`, `ResultType.Fail`, `ResultType.Exception`
- Log errors appropriately: `Logger.LogWarning()`, `Logger.LogEntryX()`

### Thread Safety
- Use `SafeLock` (custom type) or `SemaphoreSlim` for synchronization
- Use `lock` statements with `using` pattern: `using (Sync.EnterScope())`
- Use `Interlocked` for simple atomic operations
- Mark volatile fields with `volatile` keyword

### Performance
- Use `ValueTask` for hot paths
- Pool frequently allocated objects: [Pool.cs](../src/CoreOne/Pool.cs)
- Use `ArrayPool<T>` for temporary arrays
- Avoid allocations in tight loops
- Use `ImmutableList<T>` for thread-safe collections