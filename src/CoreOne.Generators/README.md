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

**Via NuGet** — no extra configuration needed. The package is automatically registered as a Roslyn analyzer:

```shell
dotnet add package CoreOne.Generators
```

**Via local project reference** — when referencing the generator directly within the same solution, mark it as an analyzer so it is not included as a runtime dependency:

```xml
<ProjectReference Include="..\CoreOne.Generators\CoreOne.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

---

# Proxy Generator (Compile-Time AOP)

The Proxy Generator enables **compile-time Aspect-Oriented Programming (AOP)**. It automatically generates a `{ClassName}Proxy` subclass that wraps every virtual method with a configurable interceptor pipeline — with zero runtime overhead from dynamic proxies or reflection at call time.

## How It Works

1. Decorate a class or individual virtual methods with `[Intercept]` to register one or more interceptors. Add `[Service]` on the class to opt it in to auto-registration.
2. The generator detects the attribute at compile time and emits a `{ClassName}Proxy` class.
3. The proxy overrides only the `virtual` / `override` methods that have at least one applicable interceptor (class-level or method-level), routing each call through the interceptor chain before (optionally) calling the base implementation.
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

### 2. Decorate the target class or individual methods

`[Intercept]` can be placed on the **class** (applies to all virtual methods), on individual **virtual methods** (applies only to that method), or both.

```csharp
using CoreOne.Attributes;

// ── Class-level: all virtual methods are intercepted ──────────────────────────

[Service(ServiceLifetime.Scoped)]
[Intercept<LoggingInterceptor>]
public class OrderService
{
    public virtual async Task<Order> CreateOrderAsync(OrderRequest request) { ... }
    public virtual Order GetOrder(int id) { ... }  // also intercepted
}

// ── Method-level: only the tagged method is intercepted ───────────────────────

[Service(ServiceLifetime.Scoped)]
public class PaymentService
{
    [Intercept<LoggingInterceptor>]
    public virtual Task<bool> ChargeAsync(decimal amount) { ... }  // intercepted

    public virtual void Refund(decimal amount) { ... }             // NOT intercepted
}

// ── Combined: class-level + method-level ─────────────────────────────────────
// Method interceptors run FIRST, then the class-level interceptors.

[Service(ServiceLifetime.Scoped)]
[Intercept<LoggingInterceptor>]       // runs second on CreateOrderAsync
public class InvoiceService
{
    [Intercept<ValidationInterceptor>] // runs first on CreateOrderAsync only
    public virtual Task<Invoice> CreateInvoiceAsync(InvoiceRequest request) { ... }

    public virtual Invoice GetInvoice(int id) { ... }  // LoggingInterceptor only
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

### Class-level only

```csharp
[Intercept<LoggingInterceptor>]
public class OrderService
{
    public virtual Task<Order> CreateOrderAsync(int id) { ... }
    public virtual Order GetOrder(int id) { ... }
}
```

Generates a shared interceptor array used by every overridden method:

```csharp
// <auto-generated/>
public partial class OrderServiceProxy : OrderService
{
    private readonly IAsyncInterceptor[] _interceptors;

    public OrderServiceProxy(LoggingInterceptor interceptor0)
    {
        _interceptors = new IAsyncInterceptor[] { interceptor0 };
    }

    public override async Task<Order> CreateOrderAsync(int id) { /* loops over _interceptors */ }
    public override Order GetOrder(int id) { /* loops over _interceptors */ }
}
```

### Method-level only

```csharp
public class PaymentService
{
    [Intercept<LoggingInterceptor>]
    public virtual Task<bool> ChargeAsync(decimal amount) { ... }

    public virtual void Refund(decimal amount) { ... }  // no attribute → not overridden
}
```

Generates a **per-method** interceptor array; the untagged method is not overridden:

```csharp
// <auto-generated/>
public partial class PaymentServiceProxy : PaymentService
{
    private readonly IAsyncInterceptor[] _methodInterceptors_ChargeAsync_Decimal_;

    public PaymentServiceProxy(LoggingInterceptor ChargeAsync_Decimal_Interceptor0)
    {
        _methodInterceptors_ChargeAsync_Decimal_ = new IAsyncInterceptor[] { ChargeAsync_Decimal_Interceptor0 };
    }

    public override async Task<bool> ChargeAsync(decimal amount) { /* loops over _methodInterceptors_ChargeAsync_Decimal_ */ }
    // Refund is NOT overridden
}
```

### Combined class-level + method-level

Method interceptors are placed **before** class interceptors in the pipeline, so they execute first.

```csharp
[Intercept<LoggingInterceptor>]
public class InvoiceService
{
    [Intercept<ValidationInterceptor>]
    public virtual Task<Invoice> CreateInvoiceAsync(InvoiceRequest request) { ... }

    public virtual Invoice GetInvoice(int id) { ... }
}
```

Generates a combined array for the tagged method and a shared array for the others:

```csharp
// <auto-generated/>
public partial class InvoiceServiceProxy : InvoiceService
{
    private readonly IAsyncInterceptor[] _interceptors;                              // class-level
    private readonly IAsyncInterceptor[] _methodInterceptors_CreateInvoiceAsync_;    // method-level + class-level combined

    public InvoiceServiceProxy(
        LoggingInterceptor interceptor0,
        ValidationInterceptor CreateInvoiceAsync_Interceptor0)
    {
        _interceptors = new IAsyncInterceptor[] { interceptor0 };
        // ValidationInterceptor first (runs first), then LoggingInterceptor
        _methodInterceptors_CreateInvoiceAsync_ = new IAsyncInterceptor[] { CreateInvoiceAsync_Interceptor0, interceptor0 };
    }

    // Uses _methodInterceptors_CreateInvoiceAsync_: ValidationInterceptor → LoggingInterceptor → base
    public override async Task<Invoice> CreateInvoiceAsync(InvoiceRequest request) { ... }

    // Uses _interceptors: LoggingInterceptor → base
    public override Invoice GetInvoice(int id) { ... }
}
```

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

Multiple interceptors are composed into a pipeline using middleware-style chaining (similar to ASP.NET Core middleware). Within a single scope (class-level or method-level), interceptors are applied **last-registered, first-executed**:

```csharp
[Intercept<LoggingInterceptor>]
[Intercept<TimingInterceptor>]
[Intercept<CachingInterceptor>]
public class ProductService
{
    public virtual Task<Product> GetProductAsync(int id) { ... }
}
// Execution order: CachingInterceptor → TimingInterceptor → LoggingInterceptor → base method
```

When both class-level and method-level interceptors are present, **method-level interceptors always execute before class-level interceptors**, regardless of declaration order:

```csharp
[Intercept<LoggingInterceptor>]        // class-level: runs second
public class OrderService
{
    [Intercept<ValidationInterceptor>] // method-level: runs first
    [Intercept<CachingInterceptor>]    // method-level: runs second (last-registered, first-executed within method scope)
    public virtual Task<Order> CreateOrderAsync(int id) { ... }
}
// Execution order on CreateOrderAsync: CachingInterceptor → ValidationInterceptor → LoggingInterceptor → base
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

## `[Intercept]` Attribute Options

| Form | Example |
|------|---------|
| Generic on class (preferred) | `[Intercept<MyInterceptor>]` |
| Non-generic on class | `[Intercept(typeof(MyInterceptor))]` |
| Generic on method | `[Intercept<MyInterceptor>]` on a virtual method |
| Non-generic on method | `[Intercept(typeof(MyInterceptor))]` on a virtual method |
| With lifetime | `[Intercept<MyInterceptor>(ServiceLifetime.Singleton)]` |
| Multiple interceptors | Stack multiple `[Intercept]` attributes (on class, method, or both) |

## Supported Method Signatures

The generator intercepts all `virtual` and `override` ordinary methods that have at least one applicable interceptor:

| Signature | Supported |
|-----------|-----------|
| `void` methods | ✅ |
| `Task` (async, no return) | ✅ |
| `Task<T>` (async with return) | ✅ |
| Synchronous return values | ✅ |
| Generic methods `Method<T>(...)` | ✅ |
| Overloaded methods | ✅ (distinguished by parameter types) |

> **Note:** Non-virtual methods are never intercepted even if tagged with `[Intercept]`. Mark methods `virtual` to include them in the proxy. A class with no virtual methods (or no virtual methods with applicable interceptors) produces no proxy output.

## DI Auto-Registration

`RegisterTypesfromAssembly<T>()` scans the assembly and automatically wires the proxy:

```csharp
// Registers OrderServiceProxy as the implementation for OrderService
services.RegisterTypesfromAssembly<OrderService>();
```

> **Important:** Only classes decorated with `[Service(ServiceLifetime)]` are eligible for auto-registration. Classes without this attribute are ignored by the scanner.

The scanner finds any type named `{OriginalName}Proxy` in the same namespace that is a subclass of the original type, and substitutes it transparently in the DI container.
