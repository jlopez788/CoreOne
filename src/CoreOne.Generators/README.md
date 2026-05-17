# CoreOne.Generators

A Roslyn source generator that produces strongly-typed IDs from a single attribute. Eliminates boilerplate for value types that wrap a primitive (e.g. `OrderId` wrapping a `Guid`).

## Usage

Decorate any `partial` type with `[StronglyTypedId]`:

```csharp
// Generic form (C# 11 / .NET 7+)
[StronglyTypedId<Guid>]
public partial struct OrderId { }

// Non-generic form (works everywhere)
[StronglyTypedId(typeof(int))]
public partial struct UserId { }
```

The generator emits a `.g.cs` file alongside your type with all members filled in automatically.

## What Gets Generated

### Core Members

| Member | Notes |
|---|---|
| `T Value` | The underlying value |
| `static T Empty` | Default/empty instance (`Guid.Empty`, `""`, etc.) |
| `string ValueAsString` | Culture-invariant string representation |
| `TypeName()` | Default constructor |
| `static TypeName From{T}(T value)` | Factory method |
| `static TypeName New()` | Guid types — wraps `Guid.NewGuid()` |
| `static TypeName Create()` | Guid types — wraps `ID.Create()` (sequential GUID) |
| `ToString()`, `GetHashCode()`, `Equals()` | Value-based equality |
| `==`, `!=` operators | Value-based comparison |
| `TryParse`, `Parse` | String parsing |
| `IParsable<T>`, `ISpanParsable<T>` | .NET 7+ only |
| `IComparable`, `<`, `<=`, `>`, `>=` | When declared on the partial type |

All members are **additive** — any member you define yourself is skipped by the generator.

### Serialization Converters

By default, the generator also emits nested converter classes and applies the corresponding attributes to your type:

| Converter | Generated Type |
|---|---|
| `System.Text.Json` | `{TypeName}JsonConverter` |
| `Newtonsoft.Json` | `{TypeName}NewtonsoftJsonConverter` |
| `System.ComponentModel.TypeConverter` | `{TypeName}TypeConverter` |
| `MongoDB.Bson` | `{TypeName}BsonConverter` |

Each converter is only emitted when the relevant library is detected in the consuming project.

## Supported Underlying Types

22 primitive types are supported:

`bool` · `byte` · `sbyte` · `short` · `ushort` · `int` · `uint` · `long` · `ulong` ·
`Int128` · `UInt128` · `BigInteger` · `float` · `double` · `decimal` · `Half` ·
`Guid` · `string` · `DateTime` · `DateTimeOffset` · `MongoDB.Bson.ObjectId`

## Attribute Options

```csharp
[StronglyTypedId<Guid>(
    generateSystemTextJsonConverter: true,           // default
    generateNewtonsoftJsonConverter: true,           // default
    generateSystemComponentModelTypeConverter: true, // default
    generateMongoDBBsonSerialization: true,          // default
    addCodeGeneratedAttribute: true,                 // default
    StringComparison = StringComparison.Ordinal,     // string IDs only
    GenerateToStringAsRecord = false
)]
public partial struct OrderId { }
```

Set any converter flag to `false` to suppress that converter entirely.

## Sequential GUIDs

For `Guid`-backed IDs, `Create()` uses `ID.Create()` from the CoreOne library, which generates sequential (v7-style) GUIDs. This is friendlier to database indexes than random GUIDs.

```csharp
var id = OrderId.Create(); // sequential GUID — good for DB inserts
var id = OrderId.New();    // random Guid.NewGuid()
```

## Setup

Add the generator project as an analyzer reference — no assembly reference is needed at runtime:

```xml
<ProjectReference Include="..\CoreOne.Generators\CoreOne.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

---

# Proxy Generator (Compile-Time AOP)

The Proxy Generator enables **compile-time Aspect-Oriented Programming (AOP)**. It automatically generates a `{ClassName}Proxy` subclass that wraps every virtual method with a configurable interceptor pipeline — with zero runtime overhead from dynamic proxies or reflection at call time.

## How It Works

1. Decorate a class with `[Service]` to opt it in to auto-registration, and `[InterceptedBy]` to register one or more interceptors.
2. The generator detects the attribute at compile time and emits a `{ClassName}Proxy` class.
3. The proxy overrides every `virtual` or `override` method, routing each call through the interceptor chain before (optionally) calling the base implementation.
4. Register your services using `RegisterTypesfromAssembly<T>()` — it automatically substitutes the proxy where the original type is expected. **A `[Service]` attribute on the class is required for auto-discovery.**

## Usage

### 1. Implement `IAsyncInterceptor`

```csharp
using CoreOne;

public class LoggingInterceptor : IAsyncInterceptor
{
    public async Task<object?> InterceptAsync(IInvocation invocation)
    {
        Console.WriteLine($"Calling {invocation.MethodName}");
        var result = await invocation.ProceedAsync();
        Console.WriteLine($"Finished {invocation.MethodName}");
        return result;
    }
}
```

### 2. Decorate the target class

```csharp
using CoreOne.Attributes;

// Generic form (preferred)
// [Service] is required for RegisterTypesfromAssembly<T>() to discover this class
[Service(ServiceLifetime.Scoped)]
[InterceptedBy<LoggingInterceptor>]
public class OrderService
{
    public virtual async Task<Order> CreateOrderAsync(OrderRequest request)
    {
        // business logic
    }
    
    public virtual Order GetOrder(int id)
    {
        // business logic
    }
}

// Non-generic form (works with older C# targets)
[Service(ServiceLifetime.Scoped)]
[InterceptedBy(typeof(LoggingInterceptor))]
public class PaymentService
{
    public virtual Task<bool> ChargeAsync(decimal amount) { ... }
}
```

### 3. Register with DI

```csharp
// [Service] on the class is required — only decorated classes are discovered
// Automatically substitutes OrderServiceProxy as the implementation for OrderService
services.RegisterTypesfromAssembly<OrderService>();

// Manual registration — inject OrderServiceProxy wherever OrderService is expected
services.AddScoped<LoggingInterceptor>();
services.AddScoped<OrderService, OrderServiceProxy>();
```

## What Gets Generated

For the following class:

```csharp
[InterceptedBy<LoggingInterceptor>]
public class OrderService
{
    public virtual Task<Order> CreateOrderAsync(int id) { ... }
}
```

The generator emits approximately:

```csharp
// <auto-generated/>
public partial class OrderServiceProxy : OrderService
{
    private static readonly MethodInfo _CreateOrderAsync_Int32Method = ...;

    private readonly IAsyncInterceptor[] _interceptors;

    public OrderServiceProxy(LoggingInterceptor interceptor0)
    {
        _interceptors = new IAsyncInterceptor[] { interceptor0 };
    }

    public override async Task<Order> CreateOrderAsync(int id)
    {
        var invocation = new Invocation
        {
            MethodName = "CreateOrderAsync",
            Method = _CreateOrderAsync_Int32Method,
            Arguments = new object[] { id },
            ProceedAsync = async () => return await base.CreateOrderAsync(id),
        };

        // Build the interceptor pipeline (last-in, first-out)
        Func<IInvocation, Task<object?>> next = (inv) => inv.ProceedAsync();
        for (int i = _interceptors.Length - 1; i >= 0; i--)
        {
            var interceptor = _interceptors[i];
            var currentNext = next;
            next = (inv) => interceptor.InterceptAsync(new Invocation { ..., ProceedAsync = () => currentNext(inv) });
        }

        var result = await next(invocation);
        return (Order)result!;
    }
}
```

## Interceptor Pipeline

Multiple interceptors are composed into a pipeline using middleware-style chaining (similar to ASP.NET Core middleware). Interceptors are applied **last-registered, first-executed**:

```csharp
[InterceptedBy<LoggingInterceptor>]
[InterceptedBy<TimingInterceptor>]
[InterceptedBy<CachingInterceptor>]
public class ProductService
{
    public virtual Task<Product> GetProductAsync(int id) { ... }
}
// Execution order: CachingInterceptor → TimingInterceptor → LoggingInterceptor → base method
```

Each interceptor receives an `IInvocation` and calls `ProceedAsync()` to advance to the next stage:

```csharp
public class TimingInterceptor : IAsyncInterceptor
{
    public async Task<object?> InterceptAsync(IInvocation invocation)
    {
        var sw = Stopwatch.StartNew();
        var result = await invocation.ProceedAsync(); // advance pipeline
        Console.WriteLine($"{invocation.MethodName} took {sw.ElapsedMilliseconds}ms");
        return result;
    }
}
```

An interceptor can **short-circuit** the pipeline by returning early without calling `ProceedAsync()`:

```csharp
public class CachingInterceptor : IAsyncInterceptor
{
    public async Task<object?> InterceptAsync(IInvocation invocation)
    {
        var key = invocation.MethodName;
        if (_cache.TryGet(key, out var cached))
            return cached; // skip base method entirely

        var result = await invocation.ProceedAsync();
        _cache.Set(key, result);
        return result;
    }
}
```

## `IInvocation` Reference

| Member | Type | Description |
|--------|------|-------------|
| `MethodName` | `string` | Name of the method being intercepted |
| `Method` | `MethodInfo` | Cached `MethodInfo` for the method |
| `Arguments` | `object[]` | Arguments passed to the method call |
| `ProceedAsync` | `Func<Task<object?>>` | Delegate that advances to the next interceptor or base method |

## `[InterceptedBy]` Attribute Options

| Form | Example |
|------|---------|
| Generic (preferred) | `[InterceptedBy<MyInterceptor>]` |
| Non-generic | `[InterceptedBy(typeof(MyInterceptor))]` |
| With lifetime | `[InterceptedBy<MyInterceptor>(ServiceLifetime.Singleton)]` |
| Multiple interceptors | Stack multiple `[InterceptedBy]` attributes |

## Supported Method Signatures

The generator intercepts all `virtual` and `override` ordinary methods including:

| Signature | Supported |
|-----------|-----------|
| `void` methods | ✅ |
| `Task` (async, no return) | ✅ |
| `Task<T>` (async with return) | ✅ |
| Synchronous return values | ✅ |
| Generic methods `Method<T>(...)` | ✅ |
| Overloaded methods | ✅ (distinguished by parameter types) |

> **Note:** Non-virtual methods are not intercepted. Mark methods `virtual` to include them in the proxy.

## DI Auto-Registration

`RegisterTypesfromAssembly<T>()` scans the assembly and automatically wires the proxy:

```csharp
// Registers OrderServiceProxy as the implementation for OrderService
services.RegisterTypesfromAssembly<OrderService>();
```

> **Important:** Only classes decorated with `[Service(ServiceLifetime)]` are eligible for auto-registration. Classes without this attribute are ignored by the scanner.

The scanner finds any type named `{OriginalName}Proxy` in the same namespace that is a subclass of the original type, and substitutes it transparently in the DI container.
