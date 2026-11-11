# CoreOne DI Attributes, AOP Interceptors, Validation & Lookups

This reference covers attribute-driven Dependency Injection registration, async AOP method interception, recursive model validation, change-tracking backing fields, and smart-enum lookup types.

---

## 1. Attribute-Driven Dependency Injection (`CoreOne.Attributes` & `CoreOne.Extensions`)

### `[Service]`, `[HostedService]`, & `[OInject]`
| Attribute | Target | Usage |
|---|---|---|
| `[Service(ServiceLifetime lifetime = Scoped, Type? servicing = null)]`<br>`[Service<TInterface>(ServiceLifetime lifetime = Scoped)]` | `class` | Marks a class for automatic DI registration via `services.AddTypesFromAssembly<TMarker>()`. If `servicing` / `TInterface` is omitted, registers the class as itself and its implemented interfaces. |
| `[HostedService]` | `class` | Marks an `IHostedService` implementation for automatic registration during `AddTypesFromAssembly<T>()`. |
| `[OInject( bool Optional = false, Type? DefaultServiceType = null)]` | `property`, `field` | Used with `ServiceInitializer.Initialize(this, services)` (automatically called by `BaseService`) to inject services into properties/fields (including `protected init` properties). |

### `ServiceCollectionExtensions` & `ServiceProviderExtensions`
```csharp
// 1. Register CoreOne defaults (Newtonsoft Settings, NJsonService, assembly scan for CoreOne types)
services.AddCoreServices();

// 2. Scan an assembly for [Service], [HostedService], and generated *Proxy classes
services.AddTypesFromAssembly<MyAssemblyMarker>();

// 3. Run a one-time async startup callback via an internal IHostedService
services.RunOnce(async (sp, ct) => await sp.GetRequiredService<IDbInitializer>().SeedAsync(ct));

// 4. Resolve a service or instantiate concrete types with DI constructor parameters via TargetCreator
var instance = serviceProvider.Resolve<MyConcreteUnregisteredClass>();
```

---

## 2. Async AOP Interception (`IAsyncInterceptor` & `[Intercept]`)

CoreOne supports compile-time proxy generation (via `CoreOne.Generators`) and DI wiring for `[Intercept]`:
- **`[Intercept(typeof(MyInterceptor), ServiceLifetime.Scoped)]`** or **`[Intercept<MyInterceptor>]`**: Can be applied to an interface, class, or individual method.
- **`IAsyncInterceptor` & `IInvocation`**:
  ```csharp
  public class TimingInterceptor(ILogger<TimingInterceptor> logger) : IAsyncInterceptor
  {
      public async Task<object?> InterceptAsync(IInvocation invocation)
      {
          var sw = Stopwatch.StartNew();
          try
          {
              // Proceed to the underlying method (or next interceptor)
              return await invocation.ProceedAsync();
          }
          catch (Exception ex)
          {
              logger.LogError(ex, "Failed in {Method}", invocation.MethodName);
              // Safely return default(T), Task.CompletedTask, or Task.FromResult(default(T))
              return invocation.GetDefaultReturnValue();
          }
      }
  }
  ```
- **Built-in `LogInterceptor`**: Logs unhandled exceptions via `OLog<LogInterceptor>` and automatically converts thrown exceptions into `Result.FromException(ex)` / `Result.FromException<T>(ex)` when the intercepted method returns `IResult` or `IResult<T>`.

---

## 3. Model Validation (`CoreOne.Attributes`, `CoreOne.Models`, `ModelExtensions`)

### Recursive Object Graph Validation: `model.ValidateModel(...)`
Unlike `Validator.TryValidateObject` which only validates top-level properties, `ModelExtensions.ValidateModel` recursively validates nested objects, collections, `IValidatableObject`, and custom `ValidationAttribute` rules while preventing infinite reference loops via `ModelValidationContext.HasSeen(instance)`:

```csharp
MValidationResult validation = requestModel.ValidateModel(serviceProvider, requireInstance: true);
if (!validation.IsValid) // or !validation.Success (MValidationResult implements IResult)
{
    string allErrors = validation.GetErrorMessages(); // Joined by ". "
    return validation.ToResult();
}
```

### Custom Validation Attributes
- **`[RequiredIf(string propertyName, object? targetValue, ComparisonType comparison = ComparisonType.EqualTo)]`**:
  Requires the decorated property when another property on the same model satisfies the comparison against `targetValue`.
  ```csharp
  [RequiredIf(nameof(PaymentType), PaymentMethod.CreditCard, ComparisonType.EqualTo)]
  public string? CardNumber { get; set; }
  ```
- **`[Comparison(string comparisonProperty, ComparisonType comparisonType)]`**:
  Compares the decorated `IComparable` property against another property on the model (e.g., `EndDate` must be `GreaterThanOrEqualTo` `StartDate`).
  ```csharp
  [Comparison(nameof(StartDate), ComparisonType.GreaterThanOrEqualTo)]
  public DateTime EndDate { get; set; }
  ```
- **`[DateValidation(AllowNull = false)]`**: Validates that a `DateTime` or `DateOnly` is after year 1900.
- **`[ExcludeFromValidation]`**: Skips recursive validation on a class, property, or field.

---

## 4. Change-Tracking Backing Fields (`BackingField<T>`)

`BackingField<T>` wraps a property value with equality comparison, previous-value tracking (`PreviousValue`, `IsChanged`), and reactive before/after notifications (`ValueChanging` / `ValueChanged` subjects):

```csharp
private readonly BackingField<string?> NameField = new("Initial");

public string? Name
{
    get => NameField.Value;
    set => NameField.UpdateValue(value); // Returns true only if value actually changed
}

// Subscribe to cancellable changing event or post-change event
NameField.SubscribeOnValueChanging(args =>
{
    if (string.IsNullOrWhiteSpace(args.NextValue))
        args.Cancel = true; // Cancels the update!
}, Token);

NameField.SubscribeOnValueChanged(args => OnNameChanged(args.CurrentValue), Token);
```

---

## 5. Type-Safe Smart Enums (`LookupType<T>` & `Policy`)

Inherit from `LookupType<T>` to create extensible, case-insensitive smart enums with automatic discovery of `public static readonly` fields (`LookupType<T>.Items` and `LookupType<T>.FindType(code)`), plus built-in JSON serialization via `LookupTypeConverter`:

```csharp
public sealed class OrderStatus : LookupType<OrderStatus>
{
    public static readonly OrderStatus Pending   = new("PENDING", "Pending Approval");
    public static readonly OrderStatus Completed = new("COMPLETED", "Order Completed");

    private OrderStatus(string code, string description) : base(code, description) { }
}

// Lookup by code (case-insensitive)
OrderStatus? status = OrderStatus.FindType("pending"); // OrderStatus.Pending
IReadOnlyList<OrderStatus> all = OrderStatus.Items;

// Equality works against both OrderStatus instances and raw strings (case-insensitive)
bool isPending = status.Equals("pending"); // true
```

### `Policy` & `PolicyCollection`
`Policy` extends `LookupType<Policy>` with an integer `Key` and bitwise `|` operator to compose `PolicyCollection` sets:
```csharp
PolicyCollection required = AppPolicies.ReadUsers | AppPolicies.WriteUsers;
bool hasAccess = userPolicies.Contains(required);
```
