# CoreOne Results (`CoreOne.Results` & `CoreOne.Extensions`)

CoreOne uses a functional, exception-safe Result pattern to represent operation outcomes, data payloads, collections, and HTTP responses without throwing exceptions across boundaries.

---

## 1. Core Interfaces & Enum (`CoreOne.Results`)

### `ResultType`
```csharp
[Flags]
public enum ResultType
{
    None = 0x10,
    Success = 0x01,
    Fail = 0x02,
    Exception = 0x04,
    Cancelled = 0x08
}
```

### Interface Hierarchy
| Interface | Base Interfaces | Properties | Purpose |
|---|---|---|---|
| `IResult` | — | `string? Message`<br>`ResultType ResultType`<br>`bool Success` (`ResultType == ResultType.Success`) | Non-generic operation outcome |
| `IResult<TModel>` | `IResult` | `TModel? Model` | Operation returning a single payload |
| `IStatusResult<TStatus>` | `IResult` | `bool IsSuccessStatusCode`<br>`TStatus StatusCode` | Operation with a status code (e.g., HTTP `int` status) |
| `IResult<TModel, TStatus>` | `IResult<TModel>`, `IStatusResult<TStatus>` | Combines `Model` and `StatusCode` | Typed payload + status code |
| `ICollectionResult<TModel>` | `IResult` | `ICollection<TModel>? Items` | Operation returning a collection of items |

---

## 2. Concrete Result Types (`CoreOne.Results`)

### `Result` & `Result<T>`
- **Static Factories**:
  - `Result.Ok` — Singleton `IResult` with `ResultType.Success` and `""` message.
  - `Result.Fail(string? message = null)` — Returns failed `IResult` (default message: `"Invalid result"`).
  - `Result.Fail<T>(string? message = null)` — Returns failed `IResult<T>`.
  - `Result.FromException(Exception ex)` / `Result.FromException<T>(Exception ex)`:
    - Maps `TaskCanceledException` / `OperationCanceledException` $\rightarrow$ `ResultType.Fail` (`"Task has been cancelled by user/app"`).
    - Maps `ObjectDisposedException` $\rightarrow$ `ResultType.Fail` (`"Object has been disposed. Can no longer use this object"`).
    - Maps `NullReferenceException` $\rightarrow$ `ResultType.Exception` (`"Null reference exception"`).
    - Maps all other exceptions $\rightarrow$ `ResultType.Exception` with `ex.InnerException?.Message ?? ex?.Message`.
- **Constructors on `Result<T>`**:
  - `new Result<T>(T? model, ResultType resultType = ResultType.Success)` — Sets `Model` and `ResultType` (allows `null` model).
  - `new Result<T>(T? model, bool requireInstance, ResultType resultType = ResultType.Success)` — When `requireInstance: true` and `model is null`, automatically downgrades to `ResultType.Fail` with `"Model is null. Invalid state"`.
  - `new Result<T>(T? model, string? message, ResultType result = ResultType.Success)`
- **Implicit Conversions on `Result<T>`**:
  - `T? -> Result<T>`: Uses `requireInstance: true` (`null` model becomes `ResultType.Fail`).
  - `Result<T> -> T?`: Extracts `response.Model` (or `default` if `response` is `null`).
  - `Result<T> -> bool`: Returns `result is not null && result.Success`.

### `CollectionResult<T>`
Implements `ICollectionResult<T>` (inherits `Result`):
```csharp
var result = new CollectionResult<User>(users); // ResultType.Success by default
var failed = new CollectionResult<User>(ResultType.Fail, "Could not load users");
```

### `HttpResult`, `HttpResult<T>`, and `HttpResult<TModel, TError>`
Represent HTTP operation outcomes (`StatusCode` is `int`):
- **Status Code Mapping (`HttpResult.GetResultFromStatusCode(int)`)**:
  - `100` or `200..299` $\rightarrow$ `(IsSuccessStatusCode: true, ResultType.Success)`
  - `400..499` $\rightarrow$ `(IsSuccessStatusCode: false, ResultType.Fail)`
  - `>= 500` $\rightarrow$ `(IsSuccessStatusCode: false, ResultType.Exception)`
  - Other $\rightarrow$ `(IsSuccessStatusCode: false, ResultType.Fail)`
- **`Success` property**: True only when `ResultType == ResultType.Success && IsSuccessStatusCode`.
- **Error Deserialization (`GetContent<TError>(ISerializer? serializer = null)`)**:
  - Deserializes `ErrorContent` (or `Message` bytes if `ErrorContent` is null) into `IResult<TError>`.
  - Supports `string`, `byte[]`, or JSON-deserialized models.
  - On `HttpResult<TModel, TError>`, if `!Success` and `ErrorModel` is already `TTarget`, returns it directly without re-deserializing.
- **Static Response Builders**:
  - `HttpResult.FromException(ex)` / `HttpResult.FromException<T>(ex)` / `HttpResult.FromException<TModel, TError>(ex)` $\rightarrow$ Status `500`.
  - `HttpResult.FromResponseAsync<TModel>(response, serializer, cancellationToken)`
  - `HttpResult.FromResponseAsync<TModel, TError>(response, serializer, cancellationToken)`
  - `HttpResult.FromJsonResponse<TModel, TError>(response, cancellationToken)`
  - `HttpResult.FromResponse<T>(response, async content => ...)`
  - `HttpResult.GetStatusCode(IResult)` / `HttpResult.GetStatusCode<T>(IResult<T>)`: Maps `ResultType.Success` (`200` or `204` if null/void), `Fail` (`400`), `Exception` (`500`).

---

## 3. Exception-Safe Wrappers (`Utility.Try` & `Utility.TryResult`)

Always prefer `Utility.Try` / `Utility.TryResult` over manual `try/catch` blocks when producing `IResult`:

| Method | Signature | Behavior |
|---|---|---|
| `Utility.Try` | `IResult Try(Action? callback)` | Executes action; returns `Result.Ok` or `Result.FromException(ex)`. |
| `Utility.Try` | `Task<IResult> Try(Func<Task>? callback)` | Awaits task; returns `Result.Ok` or `Result.FromException(ex)`. |
| `Utility.Try<T>` | `IResult<T> Try<T>(Func<T?>? callback)` | Wraps return value in `new Result<T>(model, requireInstance: true)`. Note: returning `null` yields `ResultType.Fail`! |
| `Utility.Try<T>` | `Task<IResult<T>> Try<T>(Func<Task<T?>>? callback)` | Async version of `Try<T>`; returning `null` yields `ResultType.Fail`. |
| `Utility.TryResult<T>` | `IResult<T> TryResult<T>(Func<IResult<T>> callback)` | Executes a function that already returns `IResult<T>`, catching any unhandled exception as `Result.FromException<T>(ex)`. |
| `Utility.TryResult<T>` | `Task<IResult<T>> TryResult<T>(Func<Task<IResult<T>>> callback)` | Async version of `TryResult<T>`. |

---

## 4. Fluent Pipeline Extensions (`ResultExtensions`)

> **Important Nuance**: Nearly all generic `IResult<T>` extensions treat a result as valid **only when `result.Success && result.Model is not null`**. Similarly, `ICollectionResult<T>` extensions check **`result.Success && result.Items is not null`**.

### Transforming Values: `Select` & `SelectCollection`
Maps the inner value while wrapping the callback in `Utility.Try` (exceptions in the selector automatically convert to `Exception` results, and `null` selector results become `Fail`).
```csharp
// Sync IResult / IResult<T> -> IResult<TResult>
IResult<UserDto> dtoResult = userResult.Select(user => new UserDto(user.Id, user.Name));

// Async chaining (supports Task<IResult<T>> with sync or async selector)
IResult<UserDto> dtoResult = await GetUserAsync(id)
    .SelectAsync(user => MapToDtoAsync(user));

// Collection item mapping (maps each element via SelectList)
ICollectionResult<UserDto> dtos = await GetUsersAsync()
    .SelectAsync((user, index) => new UserDto(user.Id, user.Name));

// Whole-collection transformation
ICollectionResult<UserDto> filtered = usersResult
    .SelectCollection(items => items.Where(u => u.IsActive).Select(ToDto).ToList());
```

### Flattening Nested Results: `SelectResult` (Monadic Bind / FlatMap)
Use `SelectResult` / `SelectResultAsync` when the next step in the pipeline **already returns `IResult<TResult>` or `Task<IResult<TResult>>`**.
```csharp
IResult<OrderReceipt> receipt = await GetUserAsync(userId)
    .SelectResultAsync(user => ValidateUserEligibility(user))       // returns IResult<ValidatedUser>
    .SelectResultAsync(validUser => PlaceOrderAsync(validUser));    // returns Task<IResult<OrderReceipt>>
```

### Filtering / Validation: `Where` & `WhereAsync`
Validates the model or collection against a predicate. If the predicate returns `false`, converts the result into `ResultType.Fail` with `Model = default` (or `Items = default`) and the provided error message (defaults to `"Predicate not satisfied."`).
```csharp
IResult<User> adultUser = await GetUserAsync(id)
    .WhereAsync(u => u.Age >= 18, "User must be at least 18 years old.")
    .WhereAsync(u => u.IsActive, u => $"User {u?.Name} is inactive.");

ICollectionResult<User> nonEmpty = await GetUsersAsync()
    .WhereAsync(items => items.Count > 0, "No users found.");
```

### Side Effects on Success: `OnSuccess` & `OnSuccessAsync`
Runs an action or async callback only when the result succeeded (and `Model is not null` for `IResult<T>`).
- On `IResult<T>.OnSuccessAsync(...)`, if the callback throws, it returns a failed `IResult<T>` from `Utility.Try`; if it succeeds, it preserves the original `Model`.
```csharp
IResult<User> result = await CreateUserAsync(request)
    .OnSuccessAsync(user => SendWelcomeEmailAsync(user.Email))
    .OnSuccessAsync(user => Logger.LogInformation("Created {Id}", user.Id));
```

### Side Effects & Recovery on Failure: `OnFailAsync`, `OnFailSelectAsync`, `OnFailSelectResultAsync`
Triggered when `!result.Success || result.Model is null`:
- **`OnFailAsync`**: Executes a logging/side-effect callback and returns the original failed result unchanged.
- **`OnFailSelectAsync`**: Attempts to recover by providing a fallback `T?` (wrapped in `Utility.Try`). If the fallback returns a non-null `T`, the pipeline recovers to `ResultType.Success`!
- **`OnFailSelectResultAsync`**: Attempts to recover by calling a fallback returning `IResult<T>` or `Task<IResult<T>>`.
```csharp
IResult<Config> config = await LoadRemoteConfigAsync()
    .OnFailAsync(err => Logger.LogWarning("Remote config failed: {Msg}", err.Message))
    .OnFailSelectAsync(_ => LoadDefaultLocalConfig()); // Recovers with fallback Config
```

### Always-Run Tap: `ExecuteAsync`
Runs a callback regardless of whether `IResult<T>` succeeded or failed (receiving the `IResult<T>`). If the callback itself throws an exception, returns the exception result; otherwise returns the original `result`.
```csharp
var result = await ProcessJobAsync()
    .ExecuteAsync(r => AuditLogAsync(r.ResultType, r.Message));
```

### Unwrapping at the End of a Pipeline: `Match` & `MatchAsync`
Extracts a final value from `IResult<T>` or `Task<IResult<T>>`:
```csharp
// Unwrap T with fallback value on failure/null model
User user = await GetUserAsync(id)
    .MatchAsync(onNullModel: () => User.Guest);

// Unwrap T with failure message handler
User? user = await GetUserAsync(id)
    .MatchAsync(onFailure: msg => { Logger.LogError(msg); return null; });

// Map both success and failure to a unified TResult (e.g. IActionResult / DTO)
ResponseDto response = await GetUserAsync(id)
    .MatchAsync(
        onSuccess: user => ResponseDto.Ok(user),
        onFailure: msg => ResponseDto.Error(msg ?? "Unknown error")
    );
```

### Converting Non-Generic `IResult` to `IResult<T>`: `ToResult<T>`
```csharp
IResult baseResult = ValidateInput(input);
IResult<User> typedResult = baseResult.ToResult<User>(optionalModel);
```

---

## 5. HTTP-Specific Extensions (`HttpResultExtensions`)

`HttpResult` and `HttpResult<T>` have dedicated extensions that preserve `StatusCode`, `Uri`, `ErrorContent`, and `Serializer`, and support **status-code filtering** and **typed error deserialization (`TError`)**.

### Status-Filtered & Typed-Error `OnFail` / `OnFailAsync`
All `OnFail` / `OnFailAsync` overloads on `HttpResult` and `HttpResult<T>` support:
1. Catch-all failure: `.OnFail(HttpResult<T> => ...)` or `.OnFail<T, TError>(TError? => ...)`
2. Exact status code: `.OnFail(404, ...)` or `.OnFail<T, TError>(400, errorDto => ...)`
3. Status code predicate: `.OnFail(code => code >= 500, ...)` or `.OnFail<T, TError>(code => code is 401 or 403, errorDto => ...)`

```csharp
HttpResult<Profile> profile = await apiClient.GetProfileAsync(userId)
    .OnFailAsync(404, _ => Logger.LogWarning("Profile {Id} not found", userId))
    .OnFailAsync<Profile, ApiError>(400, err => Logger.LogWarning("Validation error: {Code}", err?.Code))
    .OnFailAsync(status => status >= 500, res => AlertOpsAsync(res.Uri, res.StatusCode));
```

### HTTP Fallback Recovery: `OnFailSelect` & `OnFailSelectAsync`
Recovers from a failed `HttpResult<T>` (optionally filtered by `statusCode` or `statusPredicate`, and optionally deserializing `TError`).
- If the fallback produces a non-null `T`, returns a recovered `HttpResult<T>(model, statusCode: 200, original.Uri)`.
- If the fallback throws an exception, returns `HttpResult<T>` with status `500`.
```csharp
HttpResult<Catalog> catalog = await apiClient.GetCatalogAsync()
    .OnFailSelectAsync(status => status is 404 or >= 500, _ => GetCachedCatalog());
```

### HTTP `Select`, `SelectAsync`, & `SelectResultAsync`
- `HttpResult<T>.Select(Func<T?, TResult?>)` / `SelectAsync(...)`: Maps `Model` while preserving `StatusCode`, `Uri`, and `Serializer`. If the mapping callback throws, returns an `HttpResult<TResult>` with status `500`.
- `Task<HttpResult<T>>.SelectResultAsync(Func<T, IResult<TResult>>)`: Bridges from an HTTP call into a standard `IResult<TResult>` domain pipeline.

### HTTP `Match` & `MatchAsync`
In addition to standard `Match` signatures, `HttpResult<T>` supports inspecting the full `HttpResult<T>`, deserialized `TError?`, and `int statusCode`:
```csharp
// Match with typed error model and status code
OrderViewModel vm = await apiClient.CreateOrderAsync(request)
    .MatchAsync<Order, ProblemDetails, OrderViewModel>(
        onSuccess: order => OrderViewModel.From(order),
        onFailure: (problem, status) => OrderViewModel.Failed(status, problem?.Title)
    );
```

---

## 6. Best Practices & Anti-Patterns

- **DO use `Utility.Try` / `Utility.TryResult`** when wrapping external/IO calls that may throw exceptions.
- **DO chain async operations fluently** using `SelectAsync`, `SelectResultAsync`, `WhereAsync`, `OnSuccessAsync`, and `OnFailAsync` directly on `Task<IResult<T>>` instead of awaiting intermediate variables at every step.
- **DO remember the non-null model rule**: `IResult<T>` extension methods (`Select`, `Where`, `OnSuccess`, `Match`) treat `result.Success && result.Model is null` as a **failure/null path**. If an operation does not return a value, use non-generic `IResult` (`Result.Ok`) instead of `IResult<object?>`.
- **DON'T use `Utility.Try<T>` when `null` is a valid success value** — `Utility.Try<T>` passes `requireInstance: true` to `Result<T>`, which turns a `null` return into `ResultType.Fail`. Construct `new Result<T?>(null, ResultType.Success)` explicitly if `null` is a valid success model.
