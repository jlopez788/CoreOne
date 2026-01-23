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

### Test File Structure & Naming
```csharp
// ✅ No [TestFixture] attribute - public class without attributes
// ✅ Namespace follows Tests.{Namespace} pattern
// ✅ File name matches class under test with "Tests" suffix
namespace Tests.Extensions;

public class EnumerableExtensionsTests  // Not EnumerableExtensionsTestFixture
{
    // Tests here
}
```

### Test Method Patterns
```csharp
// ✅ Use [Test] attribute for test methods
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var list = new List<int> { 1, 2, 3 };
    
    // Act
    var result = list.AddOrUpdate(4);
    
    // Assert
    Assert.That(result, Has.Count.EqualTo(4));
}

// ✅ Use [TestCase] for parameterized tests
[TestCase(new[] { 0, 1, 2 })]
public void Each_WithIndex_ProvidesIndexToAction(int[] data)
{
    var list = new List<string> { "a", "b", "c" };
    var indices = new List<int>();
    list.Each((item, index) => indices.Add(index));
    Assert.That(indices, Is.EqualTo(data));
}

// ✅ Use [SetUp] for common initialization
[SetUp]
public void Setup()
{
    // Common setup code
}

// ✅ Use [TearDown] for cleanup
[TearDown]
public void TearDown()
{
    hub?.Dispose();
}
```

### Assertion Patterns (NUnit)
```csharp
// ✅ NUnit fluent syntax with Assert.That
Assert.That(result, Is.EqualTo(expected));
Assert.That(collection, Has.Count.EqualTo(3));
Assert.That(collection, Does.Contain(item));
Assert.That(collection, Does.Not.Contain(item));
Assert.That(collection, Is.Empty);
Assert.That(collection, Is.Not.Null);
Assert.That(result, Is.True);
Assert.That(result, Is.False);
Assert.That(result, Is.SameAs(expected));

// ✅ Multiple assertions grouped together
using (Assert.EnterMultipleScope())
{
    Assert.That(result, Is.True);
    Assert.That(set, Has.Count.EqualTo(1));
}

// ✅ Collection initialization in assertions
var set = new ConcurrentSet<int> {
    1,
    2,
    3
};

// ❌ Avoid - Old assertion style
Assert.AreEqual(expected, actual);  // Use Assert.That instead
```

### Async Test Patterns
```csharp
// ✅ Use TaskCompletionSource for synchronization
[Test]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    var tcs = new TaskCompletionSource<IMessage?>(TaskCreationOptions.RunContinuationsAsynchronously);
    var mock = new Mock<Action<IMessage>>();
    mock.Setup(m => m(It.IsAny<IMessage>())).Callback<IMessage>(m => tcs.TrySetResult(m));
    
    hub.Subscribe<IMessage>(msg => { mock.Object(msg); return Task.CompletedTask; }, null, CancellationToken.None);
    hub.Publish(msg);
    
    var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(2)));
    Assert.That(completed, Is.EqualTo(tcs.Task));
}

// ✅ Use Task.Delay for timing tests
await Task.Delay(100);  // Wait for debounce delay
Assert.That(executed, Is.True);

// ✅ Use CancellationToken for cancellable operations
var cts = new CancellationTokenSource();
hub.Subscribe<IMessage>(HandleMessage, null, cts.Token);
cts.Cancel();  // Unsubscribe
```

### Mocking with Moq
```csharp
// ✅ Mock setup with callback
var mock = new Mock<Action<IMessage>>();
mock.Setup(m => m(It.IsAny<IMessage>()))
    .Callback<IMessage>(m => tcs.TrySetResult(m));

// ✅ Verify method invocation
mock.Verify(m => m(It.IsAny<IMessage>()), Times.Once);
mock.Verify(m => m(It.IsAny<IMessage>()), Times.Never());

// ✅ Mock services for dependency injection
var mockLogger = new Mock<ILogger>();
var services = new ServiceCollection()
    .AddSingleton<ITestService, TestServiceImpl>()
    .AddSingleton<ILogger>(mockLogger.Object)
    .BuildServiceProvider();

// ⚠️ Mock types must be public (Moq limitation)
public interface IMessage { }  // Not private
public class MessageImpl : IMessage { }  // Not private
```

### Test Data & Helper Classes
```csharp
// ✅ Define test-specific types within test class
public class EnumerableExtensionsTests
{
    // Helper classes for testing
    private class TestModel
    {
        public string Name { get; set; } = "Test";
        public int Value { get; set; }
    }
    
    // Tests use helper classes
    [Test]
    public void Test_WithHelperClass()
    {
        var model = new TestModel { Value = 42 };
        // ...
    }
}

// ✅ For tests requiring public types (Moq), define at namespace level
namespace Tests;

public class HubTests
{
    // Must be public for Moq
    public interface IMessage { int Value { get; } }
    public class MessageImpl : IMessage { public int Value { get; set; } }
}
```

### Null Handling Tests
```csharp
// ✅ Always test null scenarios
[Test]
public void Method_WithNull_ReturnsEmpty()
{
    List<string>? list = null;
    var result = list.ExcludeNulls();
    Assert.That(result, Is.Empty);
}

[Test]
public void Method_NullParameter_HandlesGracefully()
{
    Assert.DoesNotThrow(() => ServiceInitializer.Initialize(instance, null));
}
```

### Edge Case & Boundary Testing
```csharp
// ✅ Test empty collections
[Test]
public void Method_WithEmptyCollection_ReturnsExpected()
{
    var list = new List<int>();
    var result = list.ExcludeNulls();
    Assert.That(result, Is.Empty);
}

// ✅ Test boundary values
[Test]
public void Debounce_ZeroDelay_ExecutesImmediately()
{
    var debounce = new Debounce(() => executed = true, TimeSpan.Zero);
    debounce.Invoke();
    await Task.Delay(10);
    Assert.That(executed, Is.True);
}

// ✅ Test duplicate handling
[Test]
public void Add_DuplicateItem_ReturnsFalse()
{
    var set = new ConcurrentSet<int> { 1 };
    var result = set.Add(1);
    Assert.That(result, Is.False);
}
```

### Thread Safety Testing
```csharp
// ✅ Test concurrent operations
[Test]
public async Task ConcurrentSet_ThreadSafety_HandlesRaceConditions()
{
    var set = new ConcurrentSet<int>();
    var tasks = Enumerable.Range(0, 100)
        .Select(i => Task.Run(() => set.Add(i)));
    
    await Task.WhenAll(tasks);
    
    Assert.That(set, Has.Count.EqualTo(100));
}
```

### Validation & Error Testing
```csharp
// ✅ Test validation success
[Test]
public void ValidateModel_ValidObject_ReturnsSuccess()
{
    var model = new ValidModel();
    var result = model.ValidateModel(null, false);
    Assert.That(result.IsValid, Is.True);
}

// ✅ Test validation failure
[Test]
public void ValidateModel_InvalidObject_ReturnsFail()
{
    var model = new InvalidModel();
    var result = model.ValidateModel(null, false);
    using (Assert.EnterMultipleScope())
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorMessages, Is.Not.Empty);
    }
}
```

### Test Organization & Coverage
- **One test class per production class**: `HubTests.cs` tests [Hub.cs](../src/CoreOne/Hubs/Hub.cs)
- **Descriptive test names**: Use `MethodName_Scenario_ExpectedBehavior` pattern
- **Test file location**: Mirror production namespace under `Tests/` folder
  - `Tests/Extensions/EnumerableExtensionsTests.cs` tests `CoreOne/Extensions/EnumerableExtensions.cs`
  - `Tests/Collections/DataTests.cs` tests `CoreOne/Collections/Data.cs`
- **Comprehensive coverage**: Test happy path, edge cases, null handling, error conditions, async behavior
- **Maintainability**: Keep tests focused, independent, and fast

### Common Test Scenarios to Cover
1. **Happy path** - Normal expected usage
2. **Null handling** - Null parameters, null collections
3. **Empty collections** - Empty lists, empty strings
4. **Edge cases** - Zero values, max values, boundary conditions
5. **Error conditions** - Invalid input, exceptions
6. **Async behavior** - Delays, cancellation, race conditions
7. **Thread safety** - Concurrent operations (for concurrent types)
8. **Inheritance/polymorphism** - Base class scenarios, interface implementations

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