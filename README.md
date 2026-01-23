# CoreOne

**A modern, high-performance C# utility library designed to make your life easier.**

[![.NET](https://img.shields.io/badge/.NET-10.0%20%7C%209.0%20%7C%20Standard%202.0%2F2.1-512BD4)](https://dotnet.microsoft.com/)
[![NuGet](https://img.shields.io/badge/NuGet-v1.3-blue)](https://www.nuget.org/packages/CoreOne)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/tests-779%20passing-brightgreen)](Tests/)
[![Coverage](https://img.shields.io/badge/coverage-57%25%20lines-yellow)](COVERAGE_REPORT.md)

CoreOne is a comprehensive utility library that provides battle-tested patterns, reactive extensions, and powerful helpers for building robust .NET applications. It eliminates boilerplate code and provides intuitive APIs for common programming tasks.

## 🚀 Why CoreOne?

### **Write Less, Do More**
Stop reinventing the wheel. CoreOne provides production-ready implementations of common patterns so you can focus on building features, not infrastructure.

### **Modern C# Features**
Built with the latest C# language features including:
- Primary constructors
- Collection expressions `[]`
- Pattern matching & switch expressions
- Nullable reference types
- `ValueTask` for high-performance async operations

### **Multi-Framework Support**
Targets .NET 10.0, .NET 9.0, .NET Standard 2.0, and 2.1 - use it anywhere.

## 📦 Installation

```bash
dotnet add package CoreOne
```

## ✨ Key Features

### 🎯 **Hub: Lightweight Event Bus**
A powerful pub/sub messaging system for decoupled communication within your application.

```csharp
// Subscribe to messages
Hub.Global.Subscribe<OrderCreated>(async order => {
    await SendConfirmationEmail(order);
});

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
}, order: 1, token);
```

**Why you'll love it:**
- ✅ Global or scoped hubs
- ✅ State management built-in
- ✅ Message interception
- ✅ Filtering and ordering
- ✅ Async-first design
- ✅ Zero external dependencies (just DI abstractions)

### 🔄 **Reactive Extensions**
Powerful observable streams inspired by ReactiveX, with familiar LINQ-like operators.

```csharp
// Create observable from events
var clicks = Observable.FromEvent<MouseEventArgs>(
    h => button.Click += h,
    h => button.Click -= h
);

// Transform streams
clicks
    .Throttle(TimeSpan.FromMilliseconds(300))  // Debounce clicks
    .Select(e => e.Location)
    .Where(loc => loc.X > 100)
    .Subscribe(location => Console.WriteLine($"Click at {location}"));

// BehaviorSubject - always has a current value
var currentUser = new BehaviorSubject<User>(User.Guest);
currentUser.Subscribe(user => UpdateUI(user));
currentUser.OnNext(authenticatedUser);
```

**Operators included:**
- `Select`, `Where`, `Distinct`, `Throttle`
- Hub integration: `hub.ToObservable<T>()`

### ✅ **Result Pattern**
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

**Features:**
- ✅ `Result.Ok`, `Result.Fail()`, `Result.FromException()`
- ✅ Generic `IResult<T>` with model payload
- ✅ Functional operators: `Select`, `PipeResult`, `OnSuccess`
- ✅ Async extensions: `SelectAsync`, `OnSuccessAsync`
- ✅ HTTP status code support via `IResult<TModel, TStatus>`

### 🗂️ **Enhanced Collections**
Type-safe, performant collections with rich APIs.

```csharp
// Data<K,V> - Dictionary with default values and fluent API
var cache = new Data<string, User> {
    DefaultKey = "guest"
};
cache.Set("admin", adminUser);
var user = cache["unknown"]; // Returns guest user via DefaultKey

// DataList<K,V> - Dictionary of lists
var usersByRole = new DataList<string, User>();
usersByRole.Add("Admin", adminUser);
usersByRole.Add("Admin", superAdmin);
var admins = usersByRole["Admin"]; // Returns list of admins

// ConcurrentSet<T> - Thread-safe set with collection initializer
var activeUsers = new ConcurrentSet<string> {
    "user1",
    "user2",
    "user3"
};
activeUsers.Add("user4"); // Thread-safe add
activeUsers.Each(user => Console.WriteLine(user)); // Safe enumeration

// ImmutableList<T> - Thread-safe collections
var observers = ImmutableList<IObserver<T>>.Empty;
observers = observers.Add(newObserver); // Creates new instance
```

### 🔧 **Rich Extension Methods**
Over **33 extension classes** covering strings, enumerables, dates, dictionaries, types, models, and more.

```csharp
// String extensions
"HelloWorld".Separate(" ") // "hello world"
"user@example.com".ContainsX("EXAMPLE") // true (case-insensitive)
"test".MatchesAny("test", "demo", "prod") // true

// Enumerable extensions
items.ExcludeNulls() // Filter out nulls
items.Each(item => Process(item)) // Iterate with action
await items.EachAsync(async item => await Process(item))
items.Partition(10) // Split into chunks
items.ToData(x => x.Id) // Convert to Data<K,V>
items.AggregateResultAsync(seed, async (acc, item) => await Process(acc, item))

// Type extensions
typeof(User).AttributeExists<RequiredAttribute>() // Check for attribute
typeof(User).GetDefault() // Get default value
typeof(User).Implements(typeof(IEntity<>)) // Check generic interface
typeof(User).IsNullable() // Check if nullable

// Member extensions
property.GetAttribute<DisplayAttribute>() // Get attribute from member
property.AttributeExists<RequiredAttribute>() // Check member attribute

// Model extensions
model.ValidateModel() // Validate with data annotations
model.ToODictionary() // Convert to dictionary

// Delegate extensions
action.AsTask() // Convert Action to Func<Task>

// Query extensions (IQueryable)
query.OrderBy("Name", SortDirection.Ascending) // Dynamic ordering
query.Paginate(page: 1, pageSize: 20) // Pagination helper

// Result extensions
result.OnSuccess(() => LogSuccess())
await result.SelectAsync(model => TransformAsync(model))
result.PipeResult(() => NextOperation())

// DateTime extensions
date.CalculateAge() // Get age from date
date.StartOfWeek() // First day of week
date.TimeAgo() // "2 hours ago"
```

### ⚡ **Async Task Queue**
Control concurrency and ensure sequential execution of async operations.

```csharp
var queue = new AsyncTaskQueue(concurrency: 3);

// Enqueue work - executes with controlled concurrency
await queue.Enqueue(async () => {
    await ProcessExpensiveOperation();
});

// Sequential processing (concurrency: 1)
var serialQueue = new AsyncTaskQueue(concurrency: 1);
await serialQueue.Enqueue(() => UpdateDatabase()); // Guaranteed order
await serialQueue.Enqueue(() => SendNotification());
```

### 🧵 **Thread Safety**
Simple, safe synchronization primitives.

```csharp
// SafeLock - simplified locking with using pattern
private readonly SafeLock Sync = new();

using (Sync.EnterScope())
{
    // Critical section - thread-safe
    _observers = _observers.Add(newObserver);
}

// Tokens for cancellation management
var token = AToken.Create(); // Auto-disposing token
var stoken = SToken.Create(); // Simple cancellation token
```

### 🔍 **Reflection Utilities**
High-performance reflection with caching.

```csharp
// Get metadata about types
var metadata = MetaType.GetMetadata<User>(nameof(User.Email));
var value = metadata.GetValue(userInstance);

// Invoke methods dynamically (cached for performance)
var invoker = MetaType.GetInvokeMethod(typeof(MyClass), "MethodName");
var result = invoker.Invoke(instance, [arg1, arg2]);

// Type utilities
Types.Parse<int>("123") // IResult<int>
Types.IsNullable<int?>() // true
var defaultValue = typeof(User).GetDefault();
```

### 📄 **Pagination & Filtering**
Built-in support for OData-style queries and pagination.

```csharp
// PageRequest for pagination and filtering
var request = new PageRequest(currentPage: 1, pageSize: 20)
    .FilterBy("Active", "Status")
    .OrderBy("Name", SortDirection.Ascending);

// Build OData queries
var builder = new ODataBuilder();
builder.Url("api/users")
    .FilterBy(BinaryOperator.Equal, $"IsActive eq true")
    .OrderBy(new OrderBy("LastName", SortDirection.Ascending))
    .Top(50);
var query = builder.ToString();
```

### 🎲 **Utilities**
Common operations made easy.

```csharp
// JSON serialization (Newtonsoft.Json)
var json = Utility.Serialize(user);
var user = Utility.DeserializeObject<User>(json);
var result = Utility.SerializeToStream(user, stream);

// Hashing
var hash = Utility.HashSHA256("password");
var crc = Utility.Crc32("data");
var encoded = Utility.ToBase64("text");

// Safe execution
var result = Utility.Try(() => RiskyOperation()); // IResult
var result = await Utility.Try(async () => await AsyncOperation());

// Phone formatting
Utility.FormatPhoneNumber("1234567890") // "(123) 456-7890"
Utility.FormatPhoneNumber("1234567890", mask: true) // "(***) ***-7890"
```

### 🎨 **Lookup Types**
Type-safe enumerations with rich metadata.

```csharp
public class OrderStatus : LookupType<OrderStatus>
{
    public static readonly OrderStatus Pending = new("PENDING", "Awaiting Processing");
    public static readonly OrderStatus Completed = new("COMPLETED", "Order Completed");
    public static readonly OrderStatus Cancelled = new("CANCELLED", "Order Cancelled");
}

// Usage
var status = OrderStatus.FindType("PENDING");
var allStatuses = OrderStatus.Items; // All defined statuses
```

### 🏗️ **Base Classes**
Foundation classes following SOLID principles.

```csharp
// Disposable - proper disposal pattern
public class MyResource : Disposable
{
    protected override void OnDispose()
    {
        // Cleanup logic
    }
}

// BaseService - DI-ready service base class
public class UserService : BaseService
{
    [Service] private IRepository<User> Repository { get; init; }
    [Service(Optional = true)] private ILogger<UserService>? Logger { get; init; }
    
    public UserService(IServiceProvider services) : base(services) { }
    
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        // Async cleanup
    }
}
```

## 🏛️ Architecture & SOLID Principles

CoreOne is built following **SOLID principles**:

- **Single Responsibility**: Each class has one focused purpose ([Hub](src/CoreOne/Hubs/Hub.cs) for messaging, [Subject](src/CoreOne/Reactive/Subject.cs) for observables)
- **Open/Closed**: Extension methods and virtual hooks allow extension without modification
- **Liskov Substitution**: Rich interface hierarchies (`IResult<T>`, `IObservable<T>`)
- **Interface Segregation**: Small, focused interfaces (`IHub`, `IObserver<T>`)
- **Dependency Inversion**: Constructor injection and service provider integration

See [.github/copilot-instructions.md](.github/copilot-instructions.md) for detailed coding guidelines.

## 📚 Documentation

### Namespaces Overview

| Namespace | Purpose |
|-----------|---------|
| `CoreOne` | Core utilities, pooling, ID generation |
| `CoreOne.Hubs` | Event bus and pub/sub messaging |
| `CoreOne.Reactive` | Observable streams and reactive extensions |
| `CoreOne.Results` | Result pattern for error handling |
| `CoreOne.Collections` | Enhanced dictionary and list types |
| `CoreOne.Extensions` | Extension methods for common types |
| `CoreOne.Reflection` | High-performance reflection utilities |
| `CoreOne.Operations` | Pagination, filtering, and query building |
| `CoreOne.Threading` | Thread safety and async utilities |
| `CoreOne.Services` | Base classes for DI-enabled services |
| `CoreOne.Lookups` | Type-safe enumeration patterns |
| `CoreOne.Attributes` | Custom attributes for DI and validation |

## 🎯 Common Scenarios

### Scenario 1: Decoupled Event Handling
```csharp
// In your order service
public class OrderService
{
    public async Task<IResult> CreateOrder(OrderRequest request)
    {
        var order = new Order(request);
        await _repository.Save(order);
        
        // Publish event - other services will react
        Hub.Global.Publish(new OrderCreated(order.Id));
        
        return Result.Ok;
    }
}

// In your email service - completely decoupled
public class EmailService
{
    public EmailService(IHub hub)
    {
        hub.Subscribe<OrderCreated>(async evt => 
            await SendOrderConfirmation(evt.OrderId));
    }
}
```

### Scenario 2: Safe API Calls
```csharp
public async Task<IResult<CustomerDto>> GetCustomerAsync(int id)
{
    return await ValidateId(id)
        .PipeResultAsync(() => _repository.GetAsync(id))
        .SelectAsync(customer => _mapper.Map(customer))
        .OnSuccessAsync(dto => _cache.SetAsync(id, dto));
}

// Clean controller action
public async Task<IActionResult> GetCustomer(int id)
{
    var result = await _service.GetCustomerAsync(id);
    return result.Success ? Ok(result.Model) : NotFound(result.Message);
}
```

### Scenario 3: Reactive UI Updates
```csharp
public class DashboardViewModel
{
    private readonly BehaviorSubject<DashboardData> _data = new(DashboardData.Empty);
    
    public IObservable<DashboardData> Data => _data;
    
    public void Initialize()
    {
        // Subscribe to real-time updates from Hub
        Hub.Global.ToObservable<DashboardData>()
            .Subscribe(data => _data.OnNext(data));
    }
}
```

## 🧪 Testing & Quality

CoreOne maintains high code quality with **779 comprehensive tests** providing **57% line coverage**:

### Coverage by Component
- **Extensions (13 classes):** 95%+ coverage
  - DelegateExtensions: 100%
  - MemberExtensions: 100%
  - ModelExtensions: 97.7%
  - ObjectExtensions: 100%
  - QueryExtensions: 95.8%
  - StringExtensions: 97.7%
  - TypeExtensions: 97.8%
  - EnumerableExtensions: 93.6%
  
- **Collections:** 95%+ coverage
  - Data<T1,T2>: 100%
  - DataList<T1,T2>: 100%
  - ConcurrentSet<T>: 95.1%
  - DataCollection: 95.4%
  
- **Core Infrastructure:** Excellent coverage
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
- **Coverage:** Coverlet MSBuild 6.0.4 with ReportGenerator
- **Patterns:** Comprehensive null handling, edge cases, thread safety, and async scenarios

See [COVERAGE_REPORT.md](COVERAGE_REPORT.md) for detailed metrics and coverage goals.

```bash
# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Generate coverage report
reportgenerator -reports:Tests/TestResults/coverage.cobertura.xml \
    -targetdir:Tests/TestResults/CoverageReport -reporttypes:Html
```

## 📋 Requirements

- **.NET 9.0+** or **.NET Standard 2.0+**
- **C# 12** (for latest features)

### Dependencies
- `Microsoft.Extensions.DependencyInjection.Abstractions` - For DI integration
- `Microsoft.Extensions.Logging.Abstractions` - For logging
- `Newtonsoft.Json` - For JSON serialization
- `System.Text.Json` - For modern JSON support

## 🤝 Contributing

Contributions are welcome! Please follow the coding guidelines in [.github/copilot-instructions.md](.github/copilot-instructions.md).

### Coding Standards
- **PascalCase** for all members (including private fields)
- **Primary constructors** preferred
- **Expression-bodied members** for simple implementations
- **Async/await** throughout
- **SOLID principles** enforced
- **Comprehensive testing** with NUnit patterns

### Testing Standards
- **Test file structure:** No `[TestFixture]` attribute, public classes only
- **Naming:** `MethodName_Scenario_ExpectedBehavior` pattern
- **Assertions:** NUnit fluent syntax with `Assert.That`
- **Async tests:** `TaskCompletionSource` for synchronization
- **Mocking:** Moq with proper callback patterns
- **Coverage areas:** Happy path, null handling, edge cases, thread safety

See the [Testing Guidelines](.github/copilot-instructions.md#testing-guidelines) section for comprehensive test patterns and examples.

## 📄 License

This project is licensed under the [MIT License](LICENSE).

## 👤 Author

**Juan Lopez**

## 🔗 Links

- [GitHub Repository](https://github.com/jlopez788/CoreOne)
- [NuGet Package](https://www.nuget.org/packages/CoreOne)

---

**Built with ❤️ using modern C# • Multi-framework support • Production-ready • Open Source**