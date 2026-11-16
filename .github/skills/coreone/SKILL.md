---
name: coreone
description: 'Guide for the CoreOne .NET library and companion packages (Http, Generators, Identity, Crypto, IO): Result pattern, Pub/Sub & State Hub, Reactive streams, Services & Lifecycle, Paging & OData, DI, Interceptors, Validation & Lookups, IDs, Extensions, Pool & Utility. Load this skill instead of reading CoreOne source files.'
---

# CoreOne Library Skill & Architecture Guide

CoreOne is a foundational .NET library providing functional operation results, an asynchronous pub/sub and state hub, lightweight reactive primitives, expression-compiled reflection/metadata, lifecycle/cancellation tokens, OData & pagination operations, attribute-driven DI & AOP interception, recursive model validation, declarative REST clients, source generators, and idiomatic extensions.

> **IMPORTANT TOKEN-SAVING RULE FOR AI AGENTS**:
> When writing features, services, consumers, or tests that use `CoreOne`, **do NOT read or grep `src/CoreOne/**` source files just to check method signatures, parameter orders, or overloads**.
> Instead, read [./references/api-signatures.md](./references/api-signatures.md) for the exact C# declarations and the domain reference guides below. Only read `src/CoreOne/**` when you are actively modifying `CoreOne`'s own internal implementation.

## Domain Reference Guides

Load the relevant reference file below based on the task you are working on:

| Area | Reference File | Key Types & Extensions | When to Load |
|------|----------------|------------------------|--------------|
| **0. Complete C# API Signature Index** | [./references/api-signatures.md](./references/api-signatures.md) | Pure C# type & method signature declarations across `CoreOne`, `CoreOne.Http`, `CoreOne.Generators`, and `CoreOne.Identity` | **Load first** whenever you need to verify exact method names, parameter orders, default values, or generic constraints without reading source code. |
| **1. Results & HTTP Results** | [./references/results.md](./references/results.md) | `IResult`, `IResult<T>`, `IStatusResult<TStatus>`, `ICollectionResult<T>`, `Result`, `Result<T>`, `CollectionResult<T>`, `HttpResult`, `HttpResult<T>`, `HttpResult<TModel, TError>`, `ResultExtensions`, `HttpResultExtensions`, `Utility.Try`, `Utility.TryResult` | Returning, transforming, filtering, recovering, or unwrapping operation/HTTP results (`Select`, `SelectResult`, `Where`, `Match`, `OnSuccess`, `OnFail`, `OnFailSelect`). |
| **2. Pub/Sub & State Hub** | [./references/hubs.md](./references/hubs.md) | `IHub`, `Hub`, `Hub.Global`, `IHubMessage`, `IGlobalHubMessage`, `IHubState<TState>`, `HubPublish<T>`, `InterceptHubMessage<T>`, `ExceptionMessage`, `HubExtensions` | Publishing messages, subscribing to events or stateful hub messages, intercepting messages, or bridging `IHub` to `IObservable<T>`. |
| **3. Reactive Streams** | [./references/reactive.md](./references/reactive.md) | `Subject<T>`, `Subject`, `BehaviorSubject<T>`, `ObserverBase<T>`, `Observer.Create`, `Observable` (`FromEvent`, `Select`, `Where`, `Distinct`, `Throttle`, `Subscribe`) | Creating hot observables, holding reactive state with `BehaviorSubject<T>`, composing `IObservable<T>` pipelines, or converting events/hubs to observables. |
| **4. Reflection & Type Utilities** | [./references/reflection.md](./references/reflection.md) | `MetaType`, `Metadata`, `InvokeCallback`, `TypeUtility`, `ServiceInitializer`, `TypeKey`, `TypeKeyStore`, `Types`, `TypeExtensions`, `MemberExtensions` | Fast expression-compiled property/field access, method invocation, constructor activation (`GetInstance`), object mapping (`BasicMap`), `[OInject]` initialization, or type/enum parsing (`Types.Parse`). |
| **5. Services, Lifecycle & Collections** | [./references/services-and-lifecycle.md](./references/services-and-lifecycle.md) | `BaseService`, `AToken`, `SToken`, `Disposable`, `Subscription`, `AsyncTaskQueue`, `Debounce`, `DebounceTrigger`, `SafeLock`, `Data<K,V>`, `DataList<K,V>`, `ConcurrentSet<T>` | Building DI services (`BaseService`, `[OInject]`), managing cancellation/disposal (`AToken`/`SToken`), queuing async tasks, debouncing, locking (`SafeLock`), or using lookup collections. |
| **6. Operations, Paging & OData** | [./references/operations-and-odata.md](./references/operations-and-odata.md) | `PageRequest`, `PageResult<T>`, `CursorRequest`, `CursorResult<T>`, `FilterBy`, `OrderBy`, `SingleOrderBy`, `QueryExtensions`, `ODataBuilder`, `ODataPageRequestBuilder`, `ODataOperator` | Building paginated or cursor-paged queries, filtering/sorting `IQueryable<T>`, or generating OData URL queries from `PageRequest`. |
| **7. DI, Interceptors, Validation & Lookups** | [./references/validation-di-and-interceptors.md](./references/validation-di-and-interceptors.md) | `[Service]`, `[HostedService]`, `[OInject]`, `AddCoreServices`, `AddTypesFromAssembly`, `[Intercept]`, `IAsyncInterceptor`, `LogInterceptor`, `[RequiredIf]`, `[Comparison]`, `ValidateModel`, `BackingField<T>`, `LookupType<T>`, `Policy` | Registering services via attributes, writing AOP async interceptors, validating object graphs, tracking field changes, or creating smart-enum `LookupType<T>` classes. |
| **8. Extensions, IDs, Pooling & Utilities** | [./references/extensions-and-utilities.md](./references/extensions-and-utilities.md) | `ID`, `ICoreId<T>`, `[StronglyTypedId<T>]`, `ToShortId`, `ToSlugUrl`, `EnumerableExtensions` (`Each`, `SelectList`, `ExcludeNulls`), `StringExtensions` (`Matches`, `IsNotNullOrEmpty`), `CloneExtensions` (`Copy`), `TaskExtensions` (`Forget`), `Pool.Rent`, `Utility` (JSON/Hash/IO) | Working with sequential GUIDs/strongly-typed IDs, collection/string/task extensions, deep cloning, array pooling, or JSON/hash/file utilities. |
| **9. Companion Packages (`Http`, `Generators`, `Identity`, `Crypto`, `IO`)** | [./references/http-generators-identity.md](./references/http-generators-identity.md) | `[RestClient]`, `[Get]`/`[Post]`, `[QueryParam]`, `[Body]`, `AddRestClient`, `[StronglyTypedId<T>]`, `ICurrentUser`, `UserIdentity`, `BearerToken`, `ITenantProvider`, `ICypher`, `CypherService`, `FileSize`, `FileType` | Defining declarative REST clients, using source generators, working with user identity/impersonation/tenancy, or using AES encryption and IO types. |

---

## Quick Decision Rules

1. **Error & Outcome Handling**: Never throw exceptions for expected domain or HTTP failures. Return `IResult`, `IResult<T>`, `ICollectionResult<T>`, or `HttpResult<T>`, and wrap throwing calls in `Utility.Try` or `Utility.TryResult`. See [./references/results.md](./references/results.md).
2. **In-Process Messaging vs Reactive Streams**:
   - Use **`IHub`** ([./references/hubs.md](./references/hubs.md)) for decoupled cross-service/component event broadcasting, ordered message interception, and keyed/named shared state (`IHubState<T>`).
   - Use **`Subject<T>` / `BehaviorSubject<T>`** ([./references/reactive.md](./references/reactive.md)) for direct observable streams, property/value streams, and operator pipelines (`Select`, `Where`, `Distinct`, `Throttle`).
3. **Cancellation & Unsubscription**:
   - CoreOne subscriptions (`IHub.Subscribe`, `Observable.Subscribe`, `Subject.Subscribe`) bind directly to a `CancellationToken` (usually `AToken` or `SToken`). When the token is cancelled/disposed (such as `BaseService.Token` during `DisposeAsync()`), all bound subscriptions automatically unsubscribe. See [./references/services-and-lifecycle.md](./references/services-and-lifecycle.md).

---

## Opt-In Project Standards (`./assets/`)

This skill bundles an optional, always-on C# instruction template at [./assets/result-standard.instructions.md](./assets/result-standard.instructions.md) (`applyTo: "**/*.cs"`) that enforces returning `IResult` over throwing exceptions across a project.
- **When asked by the user to enable or enforce the CoreOne `IResult` standard**: Copy `.github/skills/coreone/assets/result-standard.instructions.md` to `.github/instructions/result-standard.instructions.md` (or add `<CoreOneEnforceResultStandard>true</CoreOneEnforceResultStandard>` to the project's `.csproj` / `Directory.Build.props`).

