# CoreOne.Http

A lightweight, declarative REST client library for .NET, inspired by Refit. Define your API as a C# interface, decorate it with attributes, and let the `CoreOne.Generators` source generator produce a strongly-typed client for you — no reflection-based proxies, no runtime code emission.

## Getting Started

### 1. Define your API as an interface

```csharp
using CoreOne.Http.Attributes;
using CoreOne.Results;

[RestClient("/api/v1")]
[Headers("X-Api-Version: 1.0")]
public interface ICustomerApi
{
    [Get("/customers/{id}")]
    Task<HttpResult<CustomerDto>> GetCustomerAsync(int id, CancellationToken cancellationToken = default);

    [Post("/customers")]
    Task<HttpResult<CustomerDto, ProblemErrorDto>> CreateCustomerAsync(
        [Body] CustomerDto customer,
        [Authorize] string token,
        CancellationToken cancellationToken = default);

    [Delete("/customers/{id}")]
    Task<HttpResult> DeleteCustomerAsync(int id);
}
```

### 2. Register the client

```csharp
using CoreOne.Http.Extensions;

services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.com"));
```

### 3. Inject and use it

```csharp
public class CustomerService(ICustomerApi api)
{
    public async Task<CustomerDto?> GetCustomer(int id)
    {
        var result = await api.GetCustomerAsync(id);
        return result.Model;
    }
}
```

## HTTP Verb Attributes

Each method on the interface is mapped to an HTTP request with one of these attributes:

| Attribute | HTTP Method |
|---|---|
| `[Get("/path")]` | GET |
| `[Post("/path")]` | POST |
| `[Put("/path")]` | PUT |
| `[Patch("/path")]` | PATCH |
| `[Delete("/path")]` | DELETE |
| `[Head("/path")]` | HEAD |
| `[Options("/path")]` | OPTIONS |
| `[Query("/path")]` | QUERY |

## Routing

Route templates support parameter interpolation and catch-all segments:

```csharp
[Get("/customers/{id}")]                   // {id} is replaced by the `id` parameter
Task<CustomerDto> GetCustomerAsync(int id);

[Get("/files/{*path}")]                    // {*path} captures multiple segments, preserving `/`
Task<HttpResult<byte[]>> DownloadAsync(string path);
```

Use `[RestClient("/api/v1")]` on the interface to prefix all method routes with a base path. Use `[AliasAs("name")]` on a parameter when the route or form field name differs from the C# parameter name.

## Query String Parameters

Bind parameters to the query string with `[QueryParam]`. Primitives, collections, complex objects (flattened by property), and `ODataBuilder` are all supported:

```csharp
[Get("/customers/search")]
Task<HttpResult<List<CustomerDto>>> SearchAsync(
    [QueryParam] string department,                       // ?department=...
    [QueryParam("sort")] string sortBy,                   // alias: ?sort=...
    [QueryParam("odata")] ODataBuilder? odata = null,     // OData query expression
    CancellationToken cancellationToken = default);
```

An optional format string can be provided: `[QueryParam("from", "yyyy-MM-dd")] DateTime from`.

## Headers

```csharp
// Static headers — on the interface (all methods) or a single method
[Headers("X-Api-Version: 1.0", "Accept: application/json")]
public interface ICustomerApi
{
    // Dynamic header bound from a parameter
    [Get("/customers/{id}")]
    Task<CustomerDto> GetCustomerAsync(int id, [Header("X-Correlation-Id")] string correlationId);

    // Authorization header; scheme defaults to "Bearer"
    [Post("/customers")]
    Task<HttpResult<CustomerDto>> CreateAsync([Body] CustomerDto dto, [Authorize] string token);
}
```

`[Authorize("Basic")]` can be used to change the authentication scheme, and the attribute may also be placed on the interface or method level.

## Request Body

Mark a parameter with `[Body]` to send it as the request body:

```csharp
// Serialized body (JSON by default)
[Post("/customers")]
Task<HttpResult<CustomerDto>> CreateAsync([Body] CustomerDto customer);

// URL-encoded form body
[Post("/customers/form")]
Task<IResult<CustomerDto>> SubmitFormAsync([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, string> form);
```

## Multipart Uploads

Mark a method with `[Multipart]` and use the provided part types — `StreamPart`, `ByteArrayPart`, or `FileInfoPart`:

```csharp
[Multipart]
[Post("/customers/{id}/avatar")]
Task<HttpResult<string>> UploadAvatarAsync(int id, StreamPart avatar, [AliasAs("caption")] string caption);
```

```csharp
var part = new StreamPart(stream, "profile-pic.png", "image/png");
await api.UploadAvatarAsync(42, part, "My avatar");
```

Plain parameters on a multipart method are sent as regular form fields.

## Serialization

JSON serialization is built in (via `NJsonService`) and used by default. You can plug in custom `ISerializer` implementations in several ways:

```csharp
// Register a keyed serializer in DI
services.AddKeyedSingleton<ISerializer, XmlSerializerService>("xml");

// Pick a serializer per interface or per method by key…
[Serializer("xml")]
[Post("/customers/xml")]
Task<HttpResult<XmlCustomerDto>> EchoXmlAsync([Body] XmlCustomerDto customer);

// …or by type
[Serializer<XmlSerializerService>]
public interface IXmlApi { /* ... */ }
```

Serializer resolution precedence: explicit type → keyed service → default `ISerializer` from DI → built-in JSON.

A serializer key can also be set globally for a client via `[RestClient(serializerKey: "xml")]` or when registering: `AddRestClient<T>(configureClient, serializerKey: "xml")`.

## Return Types

Methods may return any of the following:

| Return type | Behavior |
|---|---|
| `Task` | Fire the request, ignore the response body |
| `Task<T>` | Deserialized response body (throws on a non-success status code) |
| `Task<string>` / `Task<byte[]>` | Raw response content |
| `Task<HttpResponseMessage>` | Full raw response, no processing |
| `Task<HttpResult>` | Status code + success/failure, no body |
| `Task<HttpResult<T>>` | Status code + deserialized model |
| `Task<HttpResult<T, TError>>` | Deserialized model on success, typed error body on failure |
| `Task<IResult>` / `Task<IResult<T>>` | CoreOne result pattern |
| `Task<Result>` / `Task<Result<T>>` | CoreOne result pattern (concrete types) |

## Dependency Injection

All registrations integrate with `IHttpClientFactory`, so the returned `IHttpClientBuilder` can be chained with handlers, Polly policies, etc.

```csharp
// Register a single client (generated implementation is resolved automatically)
services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.com"));

// Configure with access to the service provider
services.AddRestClient<ICustomerApi>((sp, client) => client.BaseAddress = sp.GetRequiredService<IOptions<ApiOptions>>().Value.BaseUri);

// Register with an explicit implementation type
services.AddRestClient<ICustomerApi, CustomerApiClient>();

// Register every generated client in an assembly at once
services.AddRestClientsFromAssembly<Program>((sp, client) => client.BaseAddress = new Uri("https://example.com"));
```

Customize the underlying `HttpClient` pipeline as usual:

```csharp
services.AddRestClient<ICustomerApi>(client => client.BaseAddress = new Uri("https://example.com"))
    .ConfigurePrimaryHttpMessageHandler(() => new MyCustomHandler());
```

A named `HttpClient` can be specified via `[RestClient(clientName: "CustomClient")]`.

## Interception

REST client interfaces compose with CoreOne's AOP pipeline. Add `[Intercept<T>]` to run an `IAsyncInterceptor` around every call:

```csharp
[Intercept<TrackingInterceptor>]
[RestClient("/api/intercepted")]
public interface IInterceptedApi
{
    [Get("/items/{id}")]
    Task<HttpResult<CustomerDto>> GetItemAsync(int id);
}
```

The generated proxy is picked up automatically by `AddRestClient` — no extra registration is needed beyond registering the interceptor itself in DI.

## Requirements

- Targets `net10.0`, `net9.0`, `netstandard2.0`, and `netstandard2.1`
- Reference `CoreOne.Generators` for source-generated client implementations
- Depends on `CoreOne` and `Microsoft.Extensions.Http`

## License

MIT
