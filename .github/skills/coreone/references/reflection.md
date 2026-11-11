# CoreOne Reflection, Metadata & Type Utilities (`CoreOne.Reflection`, `CoreOne`, `CoreOne.Extensions`)

CoreOne provides a high-performance reflection and expression-tree compilation layer (`MetaType`, `Metadata`, `InvokeCallback`, `TypeUtility`, `ServiceInitializer`, `Types`) that caches compiled property/field getters, setters, method invokers, and object activators to avoid slow repeated reflection calls.

---

## 1. Fast Property & Field Access (`MetaType` & `Metadata`)

### `Metadata` (readonly struct)
Represents cached, expression-compiled metadata for a single `PropertyInfo` or `FieldInfo`:
- **Properties**:
  - `string Name` — Member name (`string.Empty` if `Metadata.Empty`).
  - `Type FPType` — The `PropertyInfo.PropertyType` or `FieldInfo.FieldType` (`Types.Void` if empty).
  - `bool CanRead`, `bool CanWrite`, `bool IsStatic`
  - `InvokeCallback Getter`, `Set? Setter`
  - `static readonly Metadata Empty`
- **Reading & Writing Values**:
  - `object? GetValue(object? instance)` — Reads property/field using compiled expression (`instance` can be `null` for static members).
  - `bool SetValue(object? instance, object? value)` — Writes property/field using compiled expression; returns `true` if set succeeded, `false` if read-only or incompatible.
- **Attributes & Underlying MemberInfo**:
  - `T? GetCustomAttribute<T>(bool inherits = false) where T : Attribute`
  - `IEnumerable<T> GetCustomAttributes<T>(bool inherits = false) where T : Attribute`
  - `bool TryGetAttribute<T>([NotNullWhen(true)] out T? attribute) where T : Attribute`
  - `PropertyInfo? AsPropertyInfo()`, `FieldInfo? AsFieldInfo()`
  - `string GetDisplayLabel()` (extension in `MemberExtensions`): Resolves `[DisplayName]`, `[Display(Name = ...)]`, or humanized/de-camelCased member name (e.g., `"FirstName"` $\rightarrow$ `"First Name"`).

### `MetaType` (Cached Metadata & Method Compiler)
Caches `Metadata` and compiled method delegates (`InvokeCallback`) in thread-safe `ConcurrentDictionary` stores keyed by `TypeKey`:
- **Default BindingFlags**:
  - `MetaType.FLAGS` = `NonPublic | Public | Instance | Static | IgnoreCase` (default for single member lookup).
  - `MetaType.INSTANCE_FLAGS` = `Public | Instance | FlattenHierarchy` (default for class-wide `GetMetadatas`).
- **Member Metadata Lookup**:
  ```csharp
  // Single property or field (case-insensitive, includes public/non-public/static by default)
  Metadata meta = MetaType.GetMetadata(typeof(User), "Email");
  if (meta != Metadata.Empty)
  {
      object? current = meta.GetValue(user);
      meta.SetValue(user, "new@example.com");
  }

  // All public instance properties & fields on a type
  IReadOnlyList<Metadata> allMetas = MetaType.GetMetadatas(typeof(User));

  // Convert to case-insensitive Data<string, Metadata> dictionary (via EnumerableExtensions.ToDictionary)
  Data<string, Metadata> metaMap = MetaType.GetMetadatas(typeof(User)).ToDictionary();
  object? id = metaMap["Id"].GetValue(user);

  // Direct Getter / Setter delegates
  InvokeCallback getter = MetaType.GetHandler(typeof(User), "Name");
  Set? setter = MetaType.SetHandler(typeof(User), "Name");
  ```
- **Fast Compiled Method Invocation (`InvokeCallback`)**:
  Compiles `MethodInfo` calls into cached expression trees returning `InvokeCallback`:
  ```csharp
  // Instance method lookup & invocation
  InvokeCallback invoker = MetaType.GetInvokeMethod(typeof(OrderService), "CalculateTotal", typeof(int), typeof(bool));
  object? result = invoker.Invoke(orderServiceInstance, [quantity, applyDiscount]);

  // Async invocation (wraps result/Task/ValueTask in SafeTask)
  await invoker.InvokeAsync(orderServiceInstance, [quantity, applyDiscount]);

  // Static method lookup & invocation
  InvokeCallback staticInvoker = MetaType.GetInvokestaticMethod(typeof(Math), "Max", typeof(int), typeof(int));
  object? max = staticInvoker.Invoke([10, 20]);

  // From an existing MethodInfo
  InvokeCallback fromMethodInfo = MetaType.GetInvokeMethod(methodInfo);
  ```
- **Strongly-Typed Expression Name Helpers**:
  ```csharp
  // Nested property path: returns "Address.ZipCode"
  string path = MetaType.GetName(() => user.Address.ZipCode);

  // Single property name on TSource: returns "Email"
  string? propName = MetaType.GetName<User, string>(u => u.Email);

  // Extract PropertyInfo from lambda
  PropertyInfo? propInfo = MetaType.GetPropertyInfo(() => user.Email);

  // Unwrap generic/Nullable<T> to innermost underlying type
  Type? inner = MetaType.GetUnderlyingType(typeof(Task<int?>)); // typeof(int)
  ```

---

## 2. Fast Object Activation & Mapping (`TypeUtility`)

- **Compiled Constructor Activation (`GetInstance`)**:
  Compiles and caches `Expression.New` delegates by `(Type, argTypes)` so repeated instantiation is much faster than `Activator.CreateInstance`:
  ```csharp
  object instance0 = typeof(MyHandler).GetInstance();
  object instance1 = typeof(MyHandler).GetInstance(arg1);
  object instance2 = typeof(MyHandler).GetInstance(arg1, arg2);
  object instance3 = typeof(MyHandler).GetInstance(arg1, arg2, arg3);

  // Uninitialized instance (.NET 9+): bypasses constructors via RuntimeHelpers.GetUninitializedObject
  object raw = TypeUtility.GetUninitializedInstance(typeof(MyModel));
  ```
- **Recursive Property/Field Object Mapping (`BasicMap`)**:
  Copies matching properties/fields by name from `instance` to a new instance of `T` (recursively mapping nested complex objects when types differ), returning `IResult<T>`:
  ```csharp
  IResult<UserDto> mapped = TypeUtility.BasicMap<UserDto>(userEntity);
  IResult<object> mappedObj = TypeUtility.BasicMap(userEntity, typeof(UserDto));
  ```
- **Type Hierarchy Inspection**:
  - `TypeUtility.IsSubclassOfRawGeneric(typeof(BaseEntity<>), derivedType)` — Checks if `derivedType` inherits from an open generic class definition.
  - `TypeUtility.IsSystemType(type)` — Returns `true` if `type.Namespace` starts with `"System"` or `"Microsoft"`.

---

## 3. Attribute-Driven Property Injection (`ServiceInitializer`)

Used automatically by `BaseService`, or directly on any object instance to inject properties/fields marked with `[OInject]`:
```csharp
public class CustomComponent
{
    [OInject]
    protected IHub Hub { get; init; } = default!;

    [OInject(Optional = true, DefaultServiceType = typeof(SystemClock))]
    protected IClock? Clock { get; init; }
}

// Resolves all [OInject] members from IServiceProvider (including init-only backing fields)
ServiceInitializer.Initialize(componentInstance, serviceProvider);
```

---

## 4. Type Keys, Parsing & Inspection (`TypeKey`, `TypeKeyStore`, `Types`, `TypeExtensions`)

### `TypeKey` & `TypeKeyStore`
- **`TypeKey`**: Lightweight `readonly struct` combining `(Type, string Name, Type[] Arguments)` with precomputed ordinal hash code for fast dictionary lookups.
- **`TypeKeyStore`**: Thread-safe registry of known primitive/nullable `TypeKey` entries:
  - `TypeKeyStore.Register<T>(string? name = null)` / `TypeKeyStore.Register(Type? type, string? name = null)`
  - `TypeKeyStore.FindType("Int32")` or `TypeKeyStore.FindType("Int32?")`

### `Types` Static Helper (`CoreOne.Types`)
Provides cached `Type` constants (`Types.Void`, `Types.String`, `Types.Bytes`, `Types.Int`, `Types.Guid`, `Types.IResult`, `Types.Task`, etc.) and cached `TryParse` delegates:
- **`Types.IsNumberType(Type? type)`**: True for all signed/unsigned integer, float, double, and decimal types (including `Nullable<T>`).
- **`Types.GetBasicType(Type type)`**: Unwraps `Nullable<T>` to `T`.
- **`Types.Parse<T>(object? value)` / `Types.TryParse<T>(string? value, out T? parsedValue)`**:
  - Parses strings/objects into `bool` (supports `"1"`, `"true"`, `"yes"`, `"t"`, `"y"`, `"0"`, `"false"`, `"f"`, `"n"`, `"no"`), numeric types, `DateTime`, `TimeSpan`, `Guid`, or **any custom type with a public static `bool TryParse(string, out T)` method** (dynamically bound and cached!).
- **`Types.ParseEnum<T>(object? value)` / `Types.TryParseEnum<TValue>(string? input, out TValue? theEnum)`**: Case-insensitive enum parsing wrapped in `IResult<T>` or `bool`.
- **`Utility.TryChangeType<TValue>(object? value, out TValue? result, CultureInfo? cultureInfo = null)`**: General-purpose conversion supporting enums, `Guid`, `DateTimeOffset`, `IConvertible`, and `Types.Parse<TValue>` fallback.

### `TypeExtensions` & `MemberExtensions` (`CoreOne.Extensions`)
| Extension | Target | Purpose |
|---|---|---|
| `type.Implements(typeof(ISomeInterface))` | `Type?` | True if `type` is assignable to `abstractType` or implements an open generic interface (e.g. `type.Implements(typeof(IHubState<>))`). |
| `type.GetDefault()` | `Type?` | Returns compiled `default(T)` boxed as `object?` (cached per `Type`). |
| `type.IsNullable()` | `Type?` | True if `type` is `Nullable<T>`. |
| `type.IsPrimitive()` | `Type?` | True if enum, `string`, primitive value type, or in `Types.DotNetTypes` (numbers, `bool`, `DateTime`, `DateOnly`, `Guid` + nullables). |
| `type.IsGenericList()` / `type.IsLazyType()` / `type.IsAnonymous()` | `Type?` | Checks for `List<T>`, `Lazy<T>`, or compiler-generated anonymous types. |
| `type.AttributeExists<T>(inherit)` / `type.GetAttributes<T>(inherit)` | `Type` | Type-level attribute inspection. |
| `member.AttributeExists<T>(inherit)` / `member.GetAttribute<T>(inherit)` / `member.TryGetAttribute<T>(out attr, inherit)` | `MemberInfo` | Member-level attribute inspection. |
