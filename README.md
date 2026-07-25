# CoreOne

<img src="CoreOne.png" width="120" alt="CoreOne" />


**A modern, high-performance C# utility library designed to make your life easier.**

[![.NET](https://img.shields.io/badge/.NET-10.0%20%7C%209.0%20%7C%20Standard%202.0%2F2.1-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/nuget/v/CoreOne.svg)](https://www.nuget.org/packages/CoreOne)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-881%20passing-brightgreen)](Tests/)
[![Coverage](https://img.shields.io/badge/coverage-53%25%20lines-yellow)](COVERAGE_REPORT.md)

CoreOne is a comprehensive utility library that provides battle-tested patterns, reactive extensions, and powerful helpers for building robust .NET applications. It eliminates boilerplate code and provides intuitive APIs for common programming tasks.

## Why CoreOne?

### Write Less, Do More
Stop reinventing the wheel. CoreOne provides production-ready implementations of common patterns so you can focus on building features, not infrastructure.

### Modern C# Features
Built with the latest C# language features including:
- Primary constructors
- Collection expressions `[]`
- Pattern matching & switch expressions
- Nullable reference types
- `ValueTask` for high-performance async operations

### Multi-Framework Support
Targets .NET 10.0, .NET 9.0, .NET Standard 2.0, and 2.1 — use it anywhere.

## Installation

```bash
dotnet add package CoreOne
```

## Key Features

### Hub: Lightweight Event Bus
A powerful pub/sub messaging system for decoupled communication within your application.

```csharp
// Subscribe to messages
Hub.Global.Subscribe<OrderCreated>(async order => {
    await SendConfirmationEmail(order);
}, cancellationToken);

// Publish messages
Hub.Global.Publish(new OrderCreated(orderId: 123));

// State management
Hub.Global.Publish(new UserState { IsLoggedIn = true });
var currentState = Hub.Global.GetState<UserState>();

// Intercept messages before delivery
Hub.Global.Intercept<PaymentMessage>(async msg => {
    if (!await ValidatePayment(msg))
        return ResultType.Fail; // Prevent delivery
    return ResultType.Success;
}, order: 1, cancellationToken);
```

- Global or scoped hubs
- State management built-in
- Message interception with ordering
- Filtering and async-first design
- Zero external dependencies (just DI abstractions)

### Reactive Extensions
Lightweight observable streams inspired by ReactiveX, with LINQ-style operators.

```csharp
// Create observable from a .NET event by name
var clicks = Observable.FromEvent<MouseEventArgs>(button, nameof(button.Click));

// Transform streams
clicks
    .Throttle(TimeSpan.FromMilliseconds(300))  // Debounce rapid events
    .Select(e => e.Location)
    .Where(loc => loc.X > 100)
    .Subscribe(location => Console.WriteLine($"Click at {location}"), cancellationToken);

// BehaviorSubject — always has a current value
var currentUser = new BehaviorSubject<User>(User.Guest);
currentUser.Subscribe(user => UpdateUI(user), cancellationToken);
currentUser.OnNext(authenticatedUser);

// Bridge the Hub into an observable stream
Hub.Global.ToObservable<DashboardData>()
    .Subscribe(data => _subject.OnNext(data), cancellationToken);
```

Available operators: `Select`, `Select` (async), `Where`, `Distinct`, `Throttle`, `FromEvent`

### Result Pattern
Elegant error handling without exceptions. Compose operations with functional-style chaining.

```csharp
// Basic result
public IResult<User> GetUser(int id)
{
    if (id <= 0)
        return Result.Fail<User>("Invalid ID");

    var user = _repository.Find(id);
    return new Result<User>(user);
}

// Functional composition
var result = await ValidateInput(request)
    .PipeResultAsync(() => SaveToDatabase(request))
    .SelectAsync(saved => MapToDto(saved))
    .OnSuccessAsync(dto => SendNotification(dto));

if (result.Success)
    return Ok(result.Model);
else
    return BadRequest(result.Message);
```

- `Result.Ok`, `Result.Fail()`, `Result.FromException()`
- Generic `IResult<T>` with model payload
- Functional operators: `Select`, `PipeResult`, `OnSuccess`
- Async extensions: `SelectAsync`, `OnSuccessAsync`
- HTTP status code support via `IResult<TModel, TStatus>`

### Enhanced Collections
Type-safe, performant collections with rich APIs.

```csharp
// Data<K,V> — Dictionary with default-key fallback and fluent API
var cache = new Data<string, User> {
    DefaultKey = "guest"
};
cache.Set("admin", adminUser);
var user = cache["unknown"]; // Returns guest user via DefaultKey

// DataList<K,V> — Dictionary of lists
var usersByRole = new DataList<string, User>();
usersByRole.Add("Admin", adminUser);
usersByRole.Add("Admin", superAdmin);
var admins = usersByRole["Admin"]; // Returns IEnumerable<User>

// DataHashSet<K,V> — Dictionary of hash sets (unique values per key)
var tagsByItem = new DataHashSet<int, string>();
tagsByItem.Add(1, "dotnet");
tagsByItem.Add(1, "dotnet"); // Duplicate is ignored

// ConcurrentSet<T> — Thread-safe set with collection initializer syntax
var activeUsers = new ConcurrentSet<string> { "user1", "user2" };
activeUsers.Add("user3"); // Thread-safe

// CircularArray<T> — Fixed-capacity ring buffer
var recent = new CircularArray<LogEntry>(capacity: 100);
recent.Add(newEntry); // Oldest entry is overwritten when full
```

### Rich Extension Methods
27 extension classes covering strings, enumerables, dates, dictionaries, types, models, streams, tasks, and more.

```csharp
// String extensions
"HelloWorld".Separate(" ")               // "hello world"
"user@example.com".ContainsX("EXAMPLE") // true (case-insensitive)
"test".MatchesAny("test", "demo")        // true

// Enumerable extensions
items.ExcludeNulls()                     // Filter out nulls
items.Each(item => Process(item))        // Iterate with action
await items.EachAsync(async item => await Process(item))
items.Partition(10)                      // Split into chunks of 10
items.ToData(x => x.Id)                 // Convert to Data<K,V>

// Type extensions
typeof(User).AttributeExists<RequiredAttribute>()   // Check for attribute
typeof(User).Implements(typeof(IEntity<>))           // Check generic interface
typeof(User).IsNullable()                            // Check if nullable

// Model extensions
model.ValidateModel()                    // Validate with data annotations
model.ToODictionary()                   // Convert to dictionary

// Query extensions (IQueryable)
query.OrderBy("Name", SortDirection.Ascending)       // Dynamic ordering
query.Paginate(page: 1, pageSize: 20)                // Pagination helper

// Result extensions
result.OnSuccess(() => LogSuccess())
await result.SelectAsync(model => TransformAsync(model))
result.PipeResult(() => NextOperation())

// DateTime extensions
date.CalculateAge()   // Get age from date
date.StartOfWeek()    // First day of week
date.TimeAgo()        // "2 hours ago"
```

### Async Task Queue
Control concurrency and ensure ordered execution of async operations.

```csharp
// Controlled concurrency
var queue = new AsyncTaskQueue(concurrency: 3);
await queue.Enqueue(async () => await ProcessExpensiveOperation());

// Sequential processing (concurrency: 1 is default)
var serialQueue = new AsyncTaskQueue();
await serialQueue.Enqueue(() => UpdateDatabase());
await serialQueue.Enqueue(() => SendNotification());
```

### Thread Safety
Simple, safe synchronization primitives.

```csharp
// SafeLock — simplified locking with using pattern
private readonly SafeLock Sync = new();

using (Sync.EnterScope())
{
    _observers = _observers.Add(newObserver);
}

// Tokens for cancellation management
var token = AToken.Create();  // Auto-disposing token
var stoken = SToken.Create(); // Simple cancellation token
```

### Debounce
Delay and coalesce rapid calls — useful for search inputs, auto-save, and event throttling.

```csharp
// Debounce a parameterless action
var debounce = new Debounce(() => Search(query), delay: TimeSpan.FromMilliseconds(300));
debounce.Invoke(); // Resets the timer on each call

// Generic debounce with a value
var debounce = new Debounce<string>(value => Search(value), delayMS: 300);
debounce.Invoke(searchTerm);
```

### Reflection Utilities
High-performance reflection with caching.

```csharp
// Get metadata about types and members
var metadata = MetaType.GetMetadata<User>(nameof(User.Email));
var value = metadata.GetValue(userInstance);

// Invoke methods dynamically (cached for performance)
var invoker = MetaType.GetInvokeMethod(typeof(MyClass), "MethodName");
var result = invoker.Invoke(instance, [arg1, arg2]);

// Type utilities
Types.Parse<int>("123")     // IResult<int>
Types.IsNullable<int?>()    // true
typeof(User).GetDefault()   // default value for the type
```

### Pagination & Filtering
Built-in support for OData-style queries and pagination.

```csharp
// PageRequest for pagination and filtering
var request = new PageRequest(currentPage: 1, pageSize: 20)
    .FilterBy("Active", "Status")
    .OrderBy("Name", SortDirection.Ascending);

// Build OData query strings
var builder = new ODataBuilder();
builder.Url("api/users")
    .FilterBy(BinaryOperator.Equal, "IsActive eq true")
    .OrderBy(new OrderBy("LastName", SortDirection.Ascending))
    .Top(50);
var query = builder.ToString();
```

### Utilities
Common operations made easy.

```csharp
// JSON serialization (Newtonsoft.Json)
var json = Utility.Serialize(user);
var user = Utility.DeserializeObject<User>(json);

// Hashing
var hash = Utility.HashSHA256("password");
var crc = Utility.Crc32("data");

// Safe execution — wraps exceptions in IResult
var result = Utility.Try(() => RiskyOperation());
var result = await Utility.Try(async () => await AsyncOperation());

// Phone formatting
Utility.FormatPhoneNumber("1234567890")              // "(123) 456-7890"
Utility.FormatPhoneNumber("1234567890", mask: true)  // "(***) ***-7890"

// URL-safe Base64
var encoded = UrlBase64.ToUrlBase64String(model);
var decoded = UrlBase64.FromUrlBase64(encoded.Model);
```

### Sequential ID
`ID` is a strongly-typed wrapper around `Guid` that generates sequential (v7-style) GUIDs on .NET 9+ for better database index performance.

```csharp
var id = new ID();          // Sequential GUID (v7 on .NET 9+)
var empty = ID.Empty;       // Guid.Empty wrapper
bool same = id1 == id2;     // Value-based equality
```

### Cryptography
AES-based encryption and decryption with optional expiry support.

```csharp
var key = new CryptKey(keyBytes);
var cypher = new CypherService(key);

// Encrypt
string encrypted = cypher.Encrypt("sensitive data");
string withExpiry = cypher.Encrypt("token data", expiresOnUtc: DateTime.UtcNow.AddHours(1));

// Decrypt — returns IResult<string, DecryptionStatus>
var result = cypher.Decrypt(encrypted);
if (result.IsSuccessStatusCode)
    Console.WriteLine(result.Model); // "sensitive data"
```

### File Store
Persist and load typed objects to disk with optional encryption and custom serialization.

```csharp
// Basic file store
var store = new FileStore<Settings>("config.json");
store.Save(settings);
var loaded = store.Load(); // IResult<Settings>

// Encrypted file store
var encrypted = new FileStore<Settings>(cypherService, "config.enc");
encrypted.Save(settings);
```

### Compile-Time AOP / Proxy Generator
Add cross-cutting concerns (logging, caching, timing, authorization) to any class **without touching its code** — the proxy is generated at compile time by the `CoreOne.Generators` Roslyn source generator.

```csharp
// 1. Implement IAsyncInterceptor
public class LoggingInterceptor : IAsyncInterceptor
{
    public async Task<object?> InterceptAsync(IInvocation invocation)
    {
        Console.WriteLine($"→ {invocation.MethodName}");
        var result = await invocation.ProceedAsync();
        Console.WriteLine($"← {invocation.MethodName}");
        return result;
    }
}

// 2. Decorate the target class
[Service(ServiceLifetime.Scoped)]
[Intercept<LoggingInterceptor>]
public class OrderService
{
    public virtual async Task<Order> CreateOrderAsync(OrderRequest request) { ... }
    public virtual Order GetOrder(int id) { ... }
}

// 3. Register — the generated proxy is substituted automatically
services.RegisterTypesfromAssembly<OrderService>();
```

Multiple interceptors compose like middleware:

```csharp
[Service(ServiceLifetime.Scoped)]
[Intercept<CachingInterceptor>]
[Intercept<TimingInterceptor>]
[Intercept<LoggingInterceptor>]
public class ProductService
{
    public virtual Task<Product> GetProductAsync(int id) { ... }
}
// Pipeline: CachingInterceptor → TimingInterceptor → LoggingInterceptor → base method
```

A built-in `LogInterceptor` is provided that catches exceptions and returns them as `IResult` — ready to use with no additional setup.

- Zero runtime proxy overhead — proxy is compiled, not generated at runtime
- Full IDE support — generated proxy is a real C# class
- Middleware-style pipeline with short-circuit support
- Automatic DI wiring via `RegisterTypesfromAssembly<T>()` — requires `[Service]` on the class
- Supports `void`, `Task`, `Task<T>`, synchronous, and generic methods

See [CoreOne.Generators README](src/CoreOne.Generators/README.md) for full documentation.

### Strongly Typed IDs
The `CoreOne.Generators` package also includes a Roslyn source generator for strongly-typed ID wrappers — eliminating the confusion of passing raw `int` or `Guid` values across API boundaries.

```csharp
// Generic form (C# 11 / .NET 7+)
[StronglyTypedId<Guid>]
public partial struct OrderId { }

// Non-generic form (works everywhere)
[StronglyTypedId(typeof(int))]
public partial struct UserId { }
```

The generator emits value-based equality, serialization converters (System.Text.Json, Newtonsoft.Json, TypeConverter, MongoDB.Bson), `TryParse`, `IParsable<T>`, and comparison operators — all customizable via attribute flags. For `Guid`-backed types, `Create()` uses the sequential `ID` strategy for database-friendly GUIDs.

```bash
dotnet add package CoreOne.Generators
```

See [CoreOne.Generators README](src/CoreOne.Generators/README.md) for all supported types and options.

### Lookup Types
Type-safe enumerations with rich metadata.

```csharp
public class OrderStatus : LookupType<OrderStatus>
{
    public static readonly OrderStatus Pending   = new("PENDING",   "Awaiting Processing");
    public static readonly OrderStatus Completed = new("COMPLETED", "Order Completed");
    public static readonly OrderStatus Cancelled = new("CANCELLED", "Order Cancelled");
}

var status = OrderStatus.FindType("PENDING");
var all = OrderStatus.Items; // All defined statuses
```

### Base Classes
Foundation classes following SOLID principles.

```csharp
// Disposable — proper disposal pattern
public class MyResource : Disposable
{
    protected override void OnDispose()
    {
        // Cleanup logic
    }
}

// BaseService — DI-ready service base class
[Service(ServiceLifetime.Scoped)]
public class UserService : BaseService
{
    [OInject] private IRepository<User> Repository { get; init; }
    [OInject(Optional = true)] private ILogger<UserService>? Logger { get; init; }

    public UserService(IServiceProvider services) : base(services) { }
}
```

## Architecture & SOLID Principles

CoreOne is built following **SOLID principles**:

- **Single Responsibility**: Each class has one focused purpose ([Hub](src/CoreOne/Hubs/Hub.cs) for messaging, [Subject](src/CoreOne/Reactive/Subject.cs) for observables)
- **Open/Closed**: Extension methods and virtual hooks allow extension without modification
- **Liskov Substitution**: Rich interface hierarchies (`IResult<T>`, `IObservable<T>`)
- **Interface Segregation**: Small, focused interfaces (`IHub`, `IObserver<T>`)
- **Dependency Inversion**: Constructor injection and service provider integration

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for detailed coding guidelines.

## Namespaces

| Namespace | Purpose |
|-----------|---------|
| `CoreOne` | Core utilities, ID generation, URL Base64, pooling |
| `CoreOne.Hubs` | Event bus and pub/sub messaging |
| `CoreOne.Reactive` | Observable streams and reactive extensions |
| `CoreOne.Results` | Result pattern for error handling |
| `CoreOne.Collections` | Enhanced dictionary, list, set, and ring buffer types |
| `CoreOne.Extensions` | Extension methods for common types |
| `CoreOne.Reflection` | High-performance reflection utilities |
| `CoreOne.Operations` | Pagination, filtering, and OData query building |
| `CoreOne.Threading` | Thread safety and async utilities |
| `CoreOne.Services` | Base classes, debounce, file store, loading state |
| `CoreOne.Lookups` | Type-safe enumeration patterns and policy collections |
| `CoreOne.Attributes` | Custom attributes for DI, validation, and AOP |
| `CoreOne.Cryptography` | AES encryption and decryption |
| `CoreOne.Comparers` | String and reference equality comparers |
| `CoreOne.IO` | I/O utilities |
| `CoreOne.Generators` | Roslyn source generators (StronglyTypedId, Proxy/AOP) |

## Common Scenarios

### Decoupled Event Handling
```csharp
public class OrderService
{
    public async Task<IResult> CreateOrder(OrderRequest request)
    {
        var order = new Order(request);
        await _repository.Save(order);

        Hub.Global.Publish(new OrderCreated(order.Id));

        return Result.Ok;
    }
}

// Completely decoupled — no reference to OrderService
public class EmailService
{
    public EmailService(IHub hub, CancellationToken cancellationToken)
    {
        hub.Subscribe<OrderCreated>(async evt =>
            await SendOrderConfirmation(evt.OrderId), cancellationToken);
    }
}
```

### Safe API Calls
```csharp
public async Task<IResult<CustomerDto>> GetCustomerAsync(int id)
{
    return await ValidateId(id)
        .PipeResultAsync(() => _repository.GetAsync(id))
        .SelectAsync(customer => _mapper.Map(customer))
        .OnSuccessAsync(dto => _cache.SetAsync(id, dto));
}

public async Task<IActionResult> GetCustomer(int id)
{
    var result = await _service.GetCustomerAsync(id);
    return result.Success ? Ok(result.Model) : NotFound(result.Message);
}
```

### Reactive UI Updates
```csharp
public class DashboardViewModel
{
    private readonly BehaviorSubject<DashboardData> _data = new(DashboardData.Empty);

    public IObservable<DashboardData> Data => _data;

    public void Initialize(CancellationToken token)
    {
        Hub.Global.ToObservable<DashboardData>()
            .Subscribe(data => _data.OnNext(data), token);
    }
}
```

## Testing & Quality

CoreOne maintains high code quality with **881 comprehensive tests** providing **53% line coverage**.

### Coverage by Component
- **Extensions (27 classes):** 95%+ coverage
  - DelegateExtensions: 100%
  - MemberExtensions: 100%
  - ModelExtensions: 97.7%
  - ObjectExtensions: 100%
  - QueryExtensions: 95.8%
  - StringExtensions: 97.7%
  - TypeExtensions: 97.8%
  - EnumerableExtensions: 93.6%

- **Collections:** 95%+ coverage
  - Data<K,V>: 100%
  - DataList<K,V>: 100%
  - ConcurrentSet<T>: 95.1%
  - DataCollection: 95.4%

- **Core Infrastructure:**
  - Hub System: 88.9%
  - Subject<T>: 100%
  - BehaviorSubject<T>: 100%
  - Observable: 90.4%
  - Result: 100%
  - Types: 96%
  - MetaType: 92.6%

- **Services & DI:**
  - ModelTransaction: 91.7%
  - TargetCreator: 80.7%
  - BaseService: 65.3%
  - ServiceInitializer: 62.8%

### Testing Infrastructure
- **Framework:** NUnit 4.3.2 with modern async test patterns
- **Mocking:** Moq for dependency injection and callbacks
- **Coverage:** Coverlet MSBuild with ReportGenerator

See [COVERAGE_REPORT.md](COVERAGE_REPORT.md) for detailed metrics.

```bash
# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Generate coverage report
reportgenerator -reports:Tests/TestResults/coverage.cobertura.xml `
    -targetdir:Tests/TestResults/CoverageReport -reporttypes:Html
```

## Requirements

- **.NET 9.0+** or **.NET Standard 2.0+**
- **C# 12** (for latest features)

### Dependencies
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Logging.Abstractions`
- `Newtonsoft.Json`
- `System.Text.Json`
- `System.ComponentModel.Annotations`

## Contributing

Contributions are welcome! Please follow the coding guidelines in [.github/copilot-instructions.md](.github/copilot-instructions.md).

### Coding Standards
- **PascalCase** for all members (including private fields)
- **Primary constructors** preferred
- **Expression-bodied members** for simple implementations
- **Async/await** throughout
- **SOLID principles** enforced
- **Comprehensive testing** with NUnit patterns

### Testing Standards
- No `[TestFixture]` attribute — public classes only
- Naming: `MethodName_Scenario_ExpectedBehavior`
- Assertions: NUnit fluent syntax with `Assert.That`
- Async tests: `TaskCompletionSource` for synchronization
- Mocking: Moq with proper callback patterns

See the [Testing Guidelines](.github/copilot-instructions.md#testing-guidelines) for comprehensive patterns and examples.

## License

This project is licensed under the [MIT License](LICENSE).

## Author

**Juan Lopez**

## Links

- [GitHub Repository](https://github.com/jlopez788/CoreOne)
- [NuGet Package](https://www.nuget.org/packages/CoreOne)

---
