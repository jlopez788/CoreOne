# Companion Packages (`CoreOne.Http`, `CoreOne.Generators`, `CoreOne.Identity`, `Cryptography`, `IO`)

This reference covers the companion projects in the CoreOne ecosystem as well as `CoreOne.Cryptography` and `CoreOne.IO`.

---

## 1. Declarative REST Clients (`CoreOne.Http` + `CoreOne.Generators`)

`CoreOne.Http` and `CoreOne.Generators` work together to generate compile-time, zero-reflection `HttpClient` implementations from decorated C# interfaces.

### Defining a `[RestClient]` Interface
```csharp
using CoreOne.Http.Attributes;
using CoreOne.Http.Models;
using CoreOne.Results;

[RestClient("/api/v1", clientName: "OrdersApi")]
[Headers("X-Api-Version: 1.0")]
public interface IOrderApi
{
    [Get("/orders/{id}")]
    Task<HttpResult<OrderDto>> GetOrderAsync(Guid id, CancellationToken ct = default);

    [Get("/orders/search")]
    Task<HttpResult<List<OrderDto>>> SearchOrdersAsync(
        [QueryParam] string status,
        [QueryParam("from", "yyyy-MM-dd")] DateTime fromDate,
        [QueryParam("odata")] ODataBuilder? odata = null,
        CancellationToken ct = default);

    [Post("/orders")]
    Task<HttpResult<OrderDto, ProblemDetailsDto>> CreateOrderAsync(
        [Body] CreateOrderRequest request,
        [Authorize] string bearerToken,
        [Header("X-Idempotency-Key")] string idempotencyKey,
        CancellationToken ct = default);

    [Multipart]
    [Post("/orders/{id}/attachments")]
    Task<HttpResult> UploadAttachmentAsync(
        Guid id,
        StreamPart file,
        [AliasAs("description")] string description,
        CancellationToken ct = default);
}
```

### Supported Method Return Types in `[RestClient]`
`RestClientGenerator` (backed by `RestClientInvoker`) supports any of the following return types on interface methods:
- `Task<HttpResult<TModel, TError>>`
- `Task<HttpResult<TModel>>`
- `Task<HttpResult>`
- `Task<IResult<TModel>>` / `Task<Result<TModel>>`
- `Task<IResult>` / `Task<Result>`
- `Task<TModel?>`
- `Task`

### Multipart Upload Models (`CoreOne.Http.Models`)
- `new StreamPart(Stream value, string fileName, string? contentType = null, string? name = null)`
- `new ByteArrayPart(byte[] value, string fileName, string? contentType = null, string? name = null)`
- `new FileInfoPart(FileInfo value, string? fileName = null, string? contentType = null, string? name = null)`

### Registering REST Clients (`CoreOne.Http.Extensions`)
```csharp
// Auto-resolves the source-generated implementation ({InterfaceNameWithoutI}Client)
services.AddRestClient<IOrderApi>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});

// Or with IServiceProvider access
services.AddRestClient<IOrderApi>((sp, client) =>
{
    var config = sp.GetRequiredService<ApiOptions>();
    client.BaseAddress = new Uri(config.BaseUrl);
});
```

---

## 2. Source Generators (`CoreOne.Generators`)

`CoreOne.Generators` contains 3 incremental Roslyn source generators:

### A. `StronglyTypedIdSourceGenerator` (`[StronglyTypedId<T>]`)
Decorate any `partial struct`, `partial record struct`, or `partial class` with `[StronglyTypedId<T>]` (or `[StronglyTypedId(typeof(T))]`):
```csharp
[StronglyTypedId<Guid>]
public partial struct CustomerId { }
```
- **Generated Members**:
  - `T Value { get; }` and `string ValueAsString { get; }`
  - `static readonly CustomerId Empty`
  - `static CustomerId Create()` (for `Guid`: uses `ID.Create()` sequential GUID v7)
  - `static CustomerId New()` (for `Guid`: uses `Guid.NewGuid()`)
  - `static CustomerId FromGuid(Guid value)` (or `FromInt32`, `FromString`, etc.)
  - `Parse`, `TryParse`, `Equals`, `GetHashCode`, `==`, `!=`, `ToString`
  - Nested converters for `System.Text.Json`, `Newtonsoft.Json`, `TypeConverter`, and `MongoDB.Bson` (when referenced).

### B. `ProxyGenerator` (`[Service]` + `[Intercept]`)
When a service or interface uses `[Intercept<TInterceptor>]` (or `[Intercept(typeof(TInterceptor))]`), `ProxyGenerator` emits a `{ServiceName}Proxy` class that wraps method calls in `Invocation` and pipelines them through all configured `IAsyncInterceptor` instances. `services.AddTypesFromAssembly<T>()` automatically detects and registers the generated `{ServiceName}Proxy`.

### C. `RestClientGenerator` (`[RestClient]`)
Generates `{InterfaceNameWithoutLeadingI}Client` (e.g., `IOrderApi` $\rightarrow$ `OrderApiClient`) using `RestRequestBuilder` and `RestClientInvoker`.

---

## 3. Identity & Multi-Tenancy (`CoreOne.Identity`)

### Contracts & Models
- **`ICurrentUser` / `ICurrentUser<TUser>` (`where TUser : UserIdentity`)**:
  - Properties: `TUser? User`, `string? Username`, `BearerToken BearerToken`, `bool IsAuthenticated`, `bool IsExpired`, `bool IsImpersonating`.
  - Methods: `Task Initialize()`, `Task SetImpersonation(BearerToken token)`, `Task RemoveImpersonation()`.
  - Subscriptions:
    - `void Subscribe(Func<UserChangedEventArgs, Task> onNext, CancellationToken cancellationToken)`
    - `void Subscribe(Func<UserChangingEventArgs, Task> onNext, CancellationToken cancellationToken)`
- **`UserIdentity : ClaimsIdentity`**:
  - Static: `UserIdentity.Empty`, `UserIdentity.Unauthorized` (`ClaimsPrincipal`).
  - Properties: `string? Username`, `string? EmailAddress`, `DateTime? ExpiresAt`, `bool IsImpersonating`, `int[]? Permissions`, `bool IsAuthenticated`.
- **`BearerToken` (readonly struct)**:
  - `new BearerToken(string? token, string scheme = "Bearer")`, `BearerToken.Empty`, `bool IsEmpty`, `int Length`.
  - Implicitly converts to/from `string?`.
- **Hub Events (`IHubMessage`)**:
  - `UserChangingEventArgs(UserIdentity? user) : CancelEventArgs, IHubMessage` (can set `Cancel = true` to block user transition).
  - `UserChangedEventArgs(UserIdentity? user) : EventArgs, IHubMessage`.
- **Multi-Tenancy**:
  - `ITenantProvider` (`ValueTask<object?> GetTenantKey()`).
  - Attributes: `[TenantOwned]` (on entity class) and `[TenantKey]` (on tenant ID property/field).

---

## 4. Cryptography & IO (`CoreOne.Cryptography` & `CoreOne.IO`)

### `ICypher`, `CypherService`, & `PlainService` (`CoreOne.Cryptography`)
Provides AES encryption/decryption with optional UTC expiration timestamps baked into the payload:
```csharp
var key = new CryptKey("my-secret-passphrase");
var cypher = new CypherService(key);

// Encrypt with optional expiration
string encrypted = cypher.Encrypt("sensitive-payload", expiresOnUtc: DateTime.UtcNow.AddHours(1));

// Decrypt returns IResult<string, DecryptionStatus> (StatusCode: Ok, Error, Expired, InvalidData)
IResult<string, DecryptionStatus> decrypted = cypher.Decrypt(encrypted);
if (decrypted.Success)
{
    string plainText = decrypted.Model!;
}
```

### `FileStore<T>`, `FileSize`, & `FileType` (`CoreOne.Services` & `CoreOne.IO`)
- **`FileStore<T>(ICypher? cypher, ISerializer? serializer, string? path) : Disposable`**: Encrypted/serialized local file persistence for model `T`.
- **`FileSize(long size)` (readonly struct)**:
  - Implicitly converts to/from `long` bytes, supports `+` operator, and formats human-readable sizes via `fileSize.Display` or `FileSize.FormatBytes(bytes)` (e.g., `"1.5 MB"`).
- **`FileType(string? ext, string? mimeType)`**:
  - Predefined constants: `FileType.PDF`, `FileType.ZIP`, `FileType.JSON`, `FileType.XLSX`, `FileType.EMPTY`.
  - `fileType.GetRandomName()` generates a random file name with the extension.
