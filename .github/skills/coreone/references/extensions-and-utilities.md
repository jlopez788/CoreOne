# CoreOne Idiomatic Extensions, IDs, Pooling & Utilities

Always prefer CoreOne's built-in extension methods and utility primitives over verbose boilerplate when working in a CoreOne codebase.

---

## 1. Sequential IDs & Strongly-Typed IDs (`ID`, `ICoreId<T>`, `IDExtensions`)

- **`ID` (Sequential GUID Wrapper)**:
  - `ID.Create()` or `new ID()` — Generates a database-friendly sequential GUID (v7 on .NET 9+, sequential timestamp-seeded GUID on older targets).
  - `ID.Empty` — Wraps `Guid.Empty`.
  - `ID.TryParse(string? value, out ID? id)` — Parses either a standard 36-char GUID string **or** a 22-char URL-safe Base64 slug!
- **Short & Slug URL Formatting (`IDExtensions`)**:
  - `id.ToShortId()` / `guid.ToShortId()` — 22-char Base64 string with `+`, `/`, and `=` stripped.
  - `id.ToSlugUrl()` / `guid.ToSlugUrl()` — 22-char URL-safe Base64 (`-` and `_`, no padding), lossless and reversible via `ID.TryParse`.
- **`[StronglyTypedId<T>]` (`CoreOne.Attributes` + `CoreOne.Generators`)**:
  Decorate any `partial struct` with `[StronglyTypedId<Guid>]` (or `int`, `long`, `string`, etc.) to source-generate `Value`, `Empty`, `Create()` (sequential GUID), `New()`, `FromGuid(...)`, `TryParse`, `Parse`, equality operators, and JSON/TypeConverter converters:
  ```csharp
  [StronglyTypedId<Guid>]
  public partial struct OrderId { }

  var id = OrderId.Create(); // Sequential GUID
  ```

---

## 2. Collection & Enumerable Extensions (`EnumerableExtensions` & `DictionaryExtensions`)

All `EnumerableExtensions` are **null-safe** (accepting `IEnumerable<T>?` without throwing `NullReferenceException`):

| Method | Signature / Example | Why Prefer It |
|---|---|---|
| `Each` | `items.Each(x => ...)`<br>`items.Each((x, index) => ...)` | Null-safe inline iteration (with optional index) without allocating a `List<T>`. |
| `EachAsync` | `await items.EachAsync(async x => await ProcessAsync(x), ct)` | Sequential async iteration with cancellation support. |
| `ExcludeNulls` | `items.ExcludeNulls()`<br>`strings.ExcludeNullOrEmpty()` | Filters out `null` (or empty/whitespace strings) with non-nullable return type. |
| `SelectList` / `SelectArray` | `List<TDto> list = items.SelectList(x => ToDto(x))`<br>`TDto[] arr = items.SelectArray((x, i) => ToDto(x, i))` | Maps directly to `List<R>` or `R[]` in a single call instead of `.Select(...).ToList()`. |
| `ToList(predicate)` | `List<User> active = users.ToList(u => u.IsActive)` | Combines `.Where(predicate).ToList()` in one step. |
| `AddOrUpdate` | `list.AddOrUpdate(item, x => x.Id == item.Id)` | Replaces matching item in `List<T>` in-place or appends if not found. |
| `Partition` | `items.Partition(100)` | Splits sequence into chunks/batches of `partitionSize`. |
| `ToData` / `ToDataDictionary` | `items.ToDataDictionary(x => x.Id, x => x.Name)` | Converts a sequence into a safe `Data<TKey, TValue>` dictionary. |
| `AggregateResult` / `AggregateResultAsync` | `await steps.AggregateResultAsync(Result.Ok, async (acc, step) => await step.RunAsync())` | Folds over a sequence returning `IResult`, **short-circuiting immediately** when any step returns `!Success`. |
| `dict.GetSet(key, factory)` | `var val = dict.GetSet("key", () => ComputeExpensiveValue())` | Looks up `key` in any `IDictionary<K, V>`; if missing, invokes `factory()`, inserts it, and returns it. |
| `dict.GetValue(key, defaultFactory)` | `var val = dict.GetValue(nullableKey, () => DefaultVal)` | Null-safe key lookup on any `IDictionary<K, V>` without throwing `KeyNotFoundException`. |

---

## 3. String, Object, Clone & Task Extensions

### `StringExtensions`
- **`str.Matches(other)`**: Case-insensitive equality (`StringComparison.OrdinalIgnoreCase` by default). Prefer `str.Matches("admin")` over `string.Equals(str, "admin", StringComparison.OrdinalIgnoreCase)`.
- **`str.MatchesAny("a", "b", "c")`**: Returns `true` if `str` matches any candidate (case-insensitive by default).
- **`str.ContainsX(other)`**: Null-safe, case-insensitive `Contains` (`OrdinalIgnoreCase` by default).
- **`str.IsNullOrEmpty()` / `str.IsNotNullOrEmpty()`**: Null/whitespace check (`string.IsNullOrWhiteSpace`) annotated with `[NotNullWhen]` for C# nullable flow analysis!
- **`str.Separate("-")`**: Converts `PascalCase` / `camelCase` into separated lowercase words (e.g., `"OnMultiWord".Separate("-")` $\rightarrow$ `"on-multi-word"`).
- **`str.EndWith("/")`**: Appends the suffix only if `str` does not already end with it.
- **`str.SplitBy([';', ','])`**: Splits, trims each entry, and excludes empty entries.
- **`value.ToXString(format)`**: Formats any value or `Nullable<T>` using `IFormattable` when supported.

### `ObjectExtensions`, `CloneExtensions`, `ComparableExtensions`, & `TaskExtensions`
- **`obj.Copy()`** (`CloneExtensions`): Deep-clones any object graph (handling cycles and arrays) via reflection and `MemberwiseClone`.
- **`obj.IsNull()` / `obj.IsNotNull()`** (`ObjectExtensions`): Checks both `null` and `DBNull.Value` with `[NotNullWhen]` annotations.
- **`obj.ToODictionary(separator: "-")`** (`ObjectExtensions`): Converts an object's properties into an `IDictionary<string, object>` with kebab-case keys.
- **`val.Bounds(min, max)` / `val.IsBetween(min, max)` / `val.Floor(min)` / `val.Ceiling(max)`** (`ComparableExtensions`): Range clamping and checks for any `IComparable<T>`.
- **`task.Forget()`** (`TaskExtensions`): Safely fires-and-forgets a `Task` while observing/suppressing unhandled exceptions so `TaskScheduler.UnobservedTaskException` is never triggered.

---

## 4. Array Pooling & Utility Helpers (`Pool`, `Utility`, `UrlBase64`, `Crc32`)

### `Pool.Rent<T>(int size)` (`CoreOne.Pool`)
Rents a buffer from `ArrayPool<T>.Shared` wrapped in a disposable `PooledBuffer<T>` (implicitly convertible to `T[]`):
```csharp
using PooledBuffer<byte> buffer = Pool.Rent<byte>(4096);
int read = await stream.ReadAsync(buffer.Array, 0, buffer.Size, ct);
```

### `Utility` Partial Helpers (`CoreOne.Utility`)
- **JSON (`Utility.json.cs`)**:
  - `string json = Utility.Serialize(model, prettyPrint: false);`
  - `MyModel? model = Utility.DeserializeObject<MyModel>(json);`
  - `IResult<MyModel> fromStream = Utility.DeserializeObject<MyModel>(stream);`
  - `IResult toStream = Utility.SerializeToStream(model, stream);`
- **Hashing & Base64 (`Utility.hash.cs`, `UrlBase64.cs`, `Crc32.cs`)**:
  - `IResult<string> sha256 = Utility.HashSHA256(data, optionalKey);` (also `HashSHA1`, `HashMD5`)
  - `IResult<string> crcHex = Utility.Crc32(data);` / `int crcInt = Utility.Crc32AsInt(data);` / `Crc32.Compute(bytes)`
  - `IResult<string> b64 = Utility.ToBase64(bytesOrString);` / `IResult<byte[]> raw = Utility.FromBase64(b64);`
  - `IResult<string> urlB64 = UrlBase64.ToUrlBase64String(modelOrString);` / `IResult<string> decoded = UrlBase64.FromUrlBase64(urlB64);`
- **File & Path IO (`Utility.io.cs`)**:
  - `IResult<string> dir = Utility.CheckDirectory(path);` (ensures directory exists)
  - `Result copy = Utility.FCopy(sourcePath, destPath);` (buffered 512KB file copy returning `Result`)
  - `string safePath = Utility.RemoveInvalid(rawFileName);` (strips invalid path characters via binary search)
