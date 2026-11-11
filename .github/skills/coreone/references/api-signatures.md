# CoreOne Complete C# API Signature Index

> **Token-Saving Reference**: Use this file to verify exact class names, generic constraints, constructor parameters, and extension method signatures **without** reading or grepping `src/CoreOne/**`.

---

## `CoreOne.Results` & Result Extensions (`CoreOne.Extensions`)

```csharp
namespace CoreOne.Results;

[Flags] public enum ResultType { None = 0x10, Success = 0x01, Fail = 0x02, Exception = 0x04, Cancelled = 0x08 }

public interface IResult { string? Message { get; } ResultType ResultType { get; } bool Success { get; } }
public interface IResult<TModel> : IResult { TModel? Model { get; } }
public interface IStatusResult<TStatus> : IResult { bool IsSuccessStatusCode { get; } TStatus StatusCode { get; } }
public interface IResult<TModel, TStatus> : IResult<TModel>, IStatusResult<TStatus> { }
public interface ICollectionResult<TModel> : IResult { ICollection<TModel>? Items { get; } }

public class Result : IResult
{
    public static readonly IResult Ok;
    public string? Message { get; init; }
    public ResultType ResultType { get; init; }
    public bool Success => ResultType == ResultType.Success;

    public Result();
    public Result(ResultType resultType, string? message);
    public static IResult Fail(string? message = null);
    public static IResult<T> Fail<T>(string? message = null);
    public static IResult FromException(Exception ex);
    public static IResult<T> FromException<T>(Exception ex);
}

public class Result<T> : Result, IResult<T>
{
    public T? Model { get; init; }
    public Result();
    public Result(ResultType result, string? msg);
    public Result(T? model, bool requireInstance, ResultType resultType = ResultType.Success);
    public Result(T? model, ResultType resultType = ResultType.Success);
    public Result(T? model, string? message, ResultType result = ResultType.Success);

    public static implicit operator Result<T>(T? model); // uses requireInstance: true
    public static implicit operator T?(Result<T> response);
    public static implicit operator bool(Result<T> result);
}

public class CollectionResult<T> : Result, ICollectionResult<T>
{
    public ICollection<T>? Items { get; init; }
    public CollectionResult();
    public CollectionResult(ICollection<T>? items, ResultType resultType = ResultType.Success, string? message = null);
    public CollectionResult(ResultType resultType, string? message = null);
}

public class HttpResult : IResult, IStatusResult<int>
{
    public byte[]? ErrorContent { get; init; }
    public bool IsSuccessStatusCode { get; protected set; }
    public string? Message { get; set; }
    public ResultType ResultType { get; protected set; }
    public int StatusCode { get; protected set; }
    public bool Success => ResultType == ResultType.Success && IsSuccessStatusCode;
    public string? Uri { get; init; }

    public HttpResult();
    public HttpResult(HttpResponseMessage response);
    public HttpResult(int statusCode, string? msg = null);
    public virtual IResult<TError> GetContent<TError>(ISerializer? serializer = null);

    public static HttpResult FromException(Exception ex);
    public static HttpResult<T> FromException<T>(Exception ex);
    public static HttpResult<TModel, TError> FromException<TModel, TError>(Exception ex);
    public static Task<HttpResult<TModel, TError>> FromJsonResponse<TModel, TError>(HttpResponseMessage response, CancellationToken cancellationToken);
    public static Task<HttpResult<TModel>> FromResponseAsync<TModel>(HttpResponseMessage response, ISerializer serializer, CancellationToken cancellationToken = default);
    public static Task<HttpResult<TModel, TError>> FromResponseAsync<TModel, TError>(HttpResponseMessage response, ISerializer serializer, CancellationToken cancellationToken = default);
    public static Task<HttpResult<T>> FromResponse<T>(HttpResponseMessage response, Func<HttpContent, Task<T>> callback);
    public static int GetStatusCode<T>(IResult<T> result);
    public static int GetStatusCode(IResult result);
    public static (bool success, ResultType resultType) GetResultFromStatusCode(int statusCode);
}

public class HttpResult<T> : HttpResult, IResult<T>, IResult<T, int>
{
    public T? Model { get; protected set; }
    public HttpResult(T? model, int statusCode = 200, string? uri = null);
    public HttpResult(int statusCode, string? msg);
    public HttpResult<TResult> Select<TResult>(Func<T?, TResult?> next);
    public static implicit operator T?(HttpResult<T> response);
}

public class HttpResult<TModel, TError> : HttpResult, IResult<TModel>, IResult<TModel, int>
{
    public TError? ErrorModel { get; protected set; }
    public TModel? Model { get; protected set; }
    public HttpResult(TModel? model, int statusCode = 200);
    public HttpResult(TError? errorModel, int statusCode = 400);
    public HttpResult(int statusCode, string? msg);
}
```

### `ResultExtensions` & `HttpResultExtensions` (`CoreOne.Extensions`)
```csharp
public static class ResultExtensions
{
    // ExecuteAsync (always-run tap)
    public static Task<IResult<T>> ExecuteAsync<T>(this IResult<T> result, Func<IResult<T>, Task> callback);
    public static Task<IResult<T>> ExecuteAsync<T>(this Task<IResult<T>> task, Action<IResult<T>> callback);
    public static Task<IResult<T>> ExecuteAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, Task> callback);

    // Match / MatchAsync
    public static T? Match<T>(this IResult<T> result, Func<T?> onFailure);
    public static T? Match<T>(this IResult<T> result, Func<string?, T?> onFailure);
    public static TResult? Match<T, TResult>(this IResult<T> result, Func<T, TResult?> onSuccess, Func<TResult?> onFailure);
    public static Task<T> MatchAsync<T>(this Task<IResult<T>> task, Func<T> onNullModel);
    public static Task<T?> MatchAsync<T>(this Task<IResult<T>> task, Func<string?, T?> onFailure);
    public static Task<TResult> MatchAsync<T, TResult>(this Task<IResult<T>> task, Func<T, TResult> callback, Func<TResult> onNullModel);
    public static Task<TResult?> MatchAsync<T, TResult>(this Task<IResult<T>> task, Func<T, TResult?> onSuccess, Func<string?, TResult?> onFailure);

    // OnFail / Recovery
    public static Task<IResult<T>> OnFailAsync<T>(this Task<IResult<T>> task, Action<IResult<T>> callback);
    public static Task<IResult<T>> OnFailSelectAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, T?> callback);
    public static Task<IResult<T>> OnFailSelectAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, Task<T?>> callback);
    public static Task<IResult<T>> OnFailSelectResultAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, IResult<T>> callback);
    public static Task<IResult<T>> OnFailSelectResultAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, Task<IResult<T>>> callback);

    // OnSuccess
    public static IResult OnSuccess(this IResult result, Action callback);
    public static IResult<T> OnSuccess<T>(this IResult<T> result, Action<T> callback);
    public static Task<IResult> OnSuccessAsync(this IResult result, Func<Task> callback);
    public static Task<IResult> OnSuccessAsync(this Task<IResult> task, Action callback);
    public static Task<IResult> OnSuccessAsync(this Task<IResult> task, Func<Task> callback);
    public static Task<IResult<T>> OnSuccessAsync<T>(this IResult<T> result, Func<T, Task> callback);
    public static Task<IResult<T>> OnSuccessAsync<T>(this Task<IResult<T>> task, Action<T> callback);
    public static Task<IResult<T>> OnSuccessAsync<T>(this Task<IResult<T>> task, Func<T, Task> callback);

    // Select / SelectCollection / SelectResult
    public static IResult<TResult> Select<TResult>(this IResult result, Func<TResult?> callback);
    public static IResult<TResult> Select<T, TResult>(this IResult<T> result, Func<T, TResult?> callback);
    public static ICollectionResult<TResult> Select<TModel, TResult>(this ICollectionResult<TModel> result, Func<TModel, TResult> callback);
    public static ICollectionResult<TResult> Select<TModel, TResult>(this ICollectionResult<TModel> result, Func<TModel, int, TResult> callback);
    public static Task<IResult<TResult>> SelectAsync<TResult>(this IResult result, Func<Task<TResult?>> callback);
    public static Task<IResult<TResult>> SelectAsync<TResult>(this Task<IResult> task, Func<Task<TResult?>> callback);
    public static Task<IResult<TResult>> SelectAsync<T, TResult>(this IResult<T> result, Func<T, Task<TResult?>> callback);
    public static Task<IResult<TResult>> SelectAsync<T, TResult>(this Task<IResult<T>> task, Func<T, TResult?> callback);
    public static Task<IResult<TResult>> SelectAsync<T, TResult>(this Task<IResult<T>> task, Func<T, Task<TResult?>> callback);
    public static Task<ICollectionResult<TResult>> SelectAsync<TModel, TResult>(this Task<ICollectionResult<TModel>> task, Func<TModel, TResult> callback);
    public static Task<ICollectionResult<TResult>> SelectAsync<TModel, TResult>(this Task<ICollectionResult<TModel>> task, Func<TModel, int, TResult> callback);
    public static ICollectionResult<TResult> SelectCollection<TModel, TResult>(this ICollectionResult<TModel> result, Func<ICollection<TModel>, ICollection<TResult>> callback);
    public static Task<ICollectionResult<TResult>> SelectCollectionAsync<TModel, TResult>(this Task<ICollectionResult<TModel>> task, Func<ICollection<TModel>, ICollection<TResult>> callback);

    public static IResult<TResult> SelectResult<T, TResult>(this IResult<T> result, Func<T, IResult<TResult>> callback);
    public static Task<IResult<TResult>> SelectResultAsync<TResult>(this Task<IResult> task, Func<IResult<TResult>> callback);
    public static Task<IResult<TResult>> SelectResultAsync<TResult>(this Task<IResult> task, Func<Task<IResult<TResult>>> callback);
    public static Task<IResult<TResult>> SelectResultAsync<T, TResult>(this IResult<T> result, Func<T, Task<IResult<TResult>>> callback);
    public static Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<IResult<T>> task, Func<T, IResult<TResult>> callback);
    public static Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<IResult<T>> task, Func<T, Task<IResult<TResult>>> callback);

    // ToResult & Where
    public static IResult<T> ToResult<T>(this IResult result, T? model = default);
    public static IResult<T> Where<T>(this IResult<T> result, Func<T, bool> predicate, string? errorMessage = null);
    public static ICollectionResult<T> Where<T>(this ICollectionResult<T> result, Func<ICollection<T>, bool> predicate, string? errorMessage = null);
    public static Task<IResult<T>> WhereAsync<T>(this Task<IResult<T>> task, Func<T, bool> predicate, string? errorMessage = null);
    public static Task<IResult<T>> WhereAsync<T>(this Task<IResult<T>> task, Func<T, bool> predicate, Func<T?, string?>? getError = null);
    public static Task<ICollectionResult<T>> WhereAsync<T>(this Task<ICollectionResult<T>> task, Func<ICollection<T>, bool> predicate, string? errorMessage = null);
    public static Task<ICollectionResult<T>> WhereAsync<T>(this Task<ICollectionResult<T>> task, Func<ICollection<T>, bool> predicate, Func<ICollection<T>?, string?>? getError = null);
}

public static class HttpResultExtensions
{
    // Match / MatchAsync (supports HttpResult<T>, TError?, and (TError?, int statusCode))
    public static T? Match<T>(this HttpResult<T> result, Func<HttpResult<T>, T?> onFailure);
    public static T? Match<T, TError>(this HttpResult<T> result, Func<TError?, T?> onFailure);
    public static T? Match<T, TError>(this HttpResult<T> result, Func<TError?, int, T?> onFailure);
    public static TResult? Match<T, TResult>(this HttpResult<T> result, Func<T, TResult?> onSuccess, Func<HttpResult<T>, TResult?> onFailure);
    public static TResult? Match<T, TError, TResult>(this HttpResult<T> result, Func<T, TResult?> onSuccess, Func<TError?, TResult?> onFailure);
    public static TResult? Match<T, TError, TResult>(this HttpResult<T> result, Func<T, TResult?> onSuccess, Func<TError?, int, TResult?> onFailure);
    // (Async counterparts MatchAsync<...> exist on Task<HttpResult<T>> for all signatures above + Func<T> onNullModel)

    // OnFail / OnFailAsync (on HttpResult, HttpResult<T>, Task<HttpResult>, Task<HttpResult<T>>)
    // Filter variants: (callback), (int statusCode, callback), (Func<int, bool> statusPredicate, callback)
    // Callback variants: Action<HttpResult>, Func<HttpResult, Task>, Action<TError?>, Func<TError?, Task>

    // OnFailSelect / OnFailSelectAsync (on HttpResult<T> and Task<HttpResult<T>>)
    // Filter variants: (callback), (int statusCode, callback), (Func<int, bool> statusPredicate, callback)
    // Callback variants: Func<HttpResult<T>, T?>, Func<HttpResult<T>, Task<T?>>, Func<TError?, T?>, Func<TError?, Task<T?>>

    // OnSuccess / OnSuccessAsync & SelectAsync / SelectResultAsync
    public static HttpResult OnSuccess(this HttpResult result, Action callback);
    public static HttpResult<T> OnSuccess<T>(this HttpResult<T> result, Action<T> callback);
    public static Task<HttpResult> OnSuccessAsync(this HttpResult result, Func<Task> callback);
    public static Task<HttpResult> OnSuccessAsync(this Task<HttpResult> task, Action callback);
    public static Task<HttpResult> OnSuccessAsync(this Task<HttpResult> task, Func<Task> callback);
    public static Task<HttpResult<T>> OnSuccessAsync<T>(this HttpResult<T> result, Func<T, Task> callback);
    public static Task<HttpResult<T>> OnSuccessAsync<T>(this Task<HttpResult<T>> task, Action<T> callback);
    public static Task<HttpResult<T>> OnSuccessAsync<T>(this Task<HttpResult<T>> task, Func<T, Task> callback);
    public static Task<HttpResult<TResult>> SelectAsync<T, TResult>(this HttpResult<T> result, Func<T, Task<TResult?>> callback);
    public static Task<HttpResult<TResult>> SelectAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, TResult?> callback);
    public static Task<HttpResult<TResult>> SelectAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, Task<TResult?>> callback);
    public static Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, IResult<TResult>> callback);
    public static Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, Task<IResult<TResult>>> callback);
}
```

---

## `CoreOne.Hubs` & `CoreOne.Reactive`

```csharp
namespace CoreOne.Hubs;

public interface IHubMessage { }
public interface IGlobalHubMessage : IHubMessage { bool IsGlobal { get; } }
public interface IHubState<TState> : IHubMessage { static abstract TState Default { get; } string? Name { get; } }
public delegate Task<ResultType> InterceptHubMessage<TEvent>(TEvent message);
public record ExceptionMessage(Exception Exception, string Message) : IHubMessage;
public sealed class HubPublish<TEvent>(Task<TEvent> task) { public void OnComplete(Func<TEvent, Task> oncomplete); }

public interface IHub : IDisposable
{
    TState GetState<TState>(string? name = null) where TState : IHubState<TState>;
    void Intercept<TEvent>(InterceptHubMessage<TEvent> onintercept, int order, CancellationToken token);
    HubPublish<TEvent> Publish<TEvent>(TEvent message);
    void Subscribe<TEvent>(Func<TEvent, Task> onmessage, Predicate<TEvent>? filter, CancellationToken token);
    void SubscribeState<TEvent>(string? name, Func<TEvent, Task> onstate, Predicate<TEvent>? filter, CancellationToken token) where TEvent : IHubState<TEvent>;
}

[Service]
public class Hub : Disposable, IHub { public static readonly Hub Global; public Hub(); }

// HubExtensions (namespace CoreOne.Extensions)
public static class HubExtensions
{
    public static void Intercept<TEvent>(this IHub hub, InterceptHubMessage<TEvent> onintercept, CancellationToken cancellationToken);
    public static void OnComplete<TEvent>(this HubPublish<TEvent> publish, Action<TEvent> oncomplete);
    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent> onmessage, CancellationToken cancellationToken);
    public static void Subscribe<TEvent>(this IHub hub, Func<TEvent, Task> onmessage, CancellationToken cancellationToken);
    public static void Subscribe<TEvent>(this IHub hub, Action? onmessage, Predicate<TEvent>? filter, CancellationToken cancellationToken);
    public static void Subscribe<TEvent>(this IHub hub, Action<TEvent>? onmessage, Predicate<TEvent>? filter, CancellationToken cancellationToken);
    public static void SubscribeState<TEvent>(this IHub hub, Action<TEvent> onstate, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>;
    public static void SubscribeState<TEvent>(this IHub hub, Action<TEvent> onstate, Predicate<TEvent>? filter, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>;
    public static void SubscribeState<TEvent>(this IHub hub, string name, Action<TEvent> onstate, Predicate<TEvent>? filter, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>;
    public static void SubscribeState<TEvent>(this IHub hub, Func<TEvent, Task> onstate, Predicate<TEvent>? filter, CancellationToken cancellationToken) where TEvent : IHubState<TEvent>;
    public static IObservable<TEvent> ToObservable<TEvent>(this IHub hub);
}
```

```csharp
namespace CoreOne.Reactive;

public abstract class ObserverBase<T> : Disposable, IObserver<T>
{
    protected Exception? Exception { get; }
    public void OnNext(T value); public void OnError(Exception exception); public void OnCompleted();
    protected abstract void OnNextCore(T value);
    protected virtual void OnCompletedCore(); protected virtual void OnErrorCore(Exception exception);
}

public class Subject<T> : ObserverBase<T>, IObserver<T>, IObservable<T>
{
    public bool HasObservers { get; }
    public IDisposable Subscribe(IObserver<T> observer);
    protected virtual void OnSubscribe(IObserver<T> observer);
}

public sealed class BehaviorSubject<T>(T? value = default) : Subject<T>
{
    public T? Value { get; }
    public bool TryGetValue([NotNullWhen(true)] out T? value);
}

public class Subject // Parameterless void signal
{
    public bool HasObservers { get; }
    public void Publish();
    public void Subscribe(Func<Task> callback, CancellationToken cancellationToken);
    public void Subscribe(Action callback, CancellationToken cancellationToken);
}

public static class Observer
{
    public static IObserver<T> Create<T>(Action<T> onNext, Action<Exception>? onError = null, Action? onCompleted = null);
}

public static class Observable
{
    public static IObservable<TEventArgs> FromEvent<TEventArgs>(object target, string eventName) where TEventArgs : EventArgs;
    public static IObservable<TSource> Distinct<TSource>(this IObservable<TSource> source, IEqualityComparer<TSource>? comparer = null);
    public static IObservable<TSource> Distinct<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer = null);
    public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector);
    public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> selector);
    public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, int durationMs);
    public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, TimeSpan duration);
    public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate);

    public static void Subscribe<T>(this IObservable<T> source, Action<T> onNext, CancellationToken token);
    public static void Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action? oncomplete, CancellationToken token);
    public static void Subscribe<T>(this IObservable<T> source, Func<T, Task> onNext, CancellationToken token);
    public static void Subscribe<T>(this IObservable<T> source, Func<T, Task> onNext, Action? oncomplete, CancellationToken token);
    public static void Subscribe<T>(this IObservable<T> source, IObserver<T> observer, CancellationToken token);
    public static void Subscribe(this IObservable<EventArgs> source, Action onNext, CancellationToken cancellationToken);
}
```

---

## `CoreOne` Primitives, Services, Collections & Reflection

```csharp
namespace CoreOne;

public class Disposable : IDisposable { public static readonly Disposable Empty; public bool IsDisposed { get; } public void Dispose(); protected virtual void OnDispose(); }
public sealed class Subscription(Action callback) : IDisposable { public void Dispose(); }
public class AToken : IAsyncDisposable
{
    public static readonly AToken Empty;
    public string Id { get; } public bool IsCancellationRequested { get; }
    public static AToken Create();
    public void Register(IDisposable disposable); public void Register(Action callback); public void Register(Func<Task> callback); public void RegisterAsync(IAsyncDisposable asyncDisposable);
    public ValueTask DisposeAsync();
    public static implicit operator CancellationToken(AToken? subscriberToken);
    public static implicit operator SToken(AToken? token);
}
public class SToken : IComponent, IDisposable
{
    public static readonly SToken Empty;
    public string Id { get; } public bool IsCancellationRequested { get; }
    public static SToken Create();
    public static SToken CreateLinkedTokens(params CancellationToken[] tokens);
    public void Cancel(); public void Dispose();
    public void Register(IDisposable disposable); public void Register(Action? callback = null); public void Register(Action<object?> callback, object state);
    public static implicit operator CancellationToken(SToken? subscriberToken);
}
public class AsyncTaskQueue(int concurrency = 1)
{
    public Task Enqueue(Action callback, CancellationToken cancellationToken = default);
    public Task Enqueue(Func<Task> callback, CancellationToken cancellationToken = default);
    public Task<T> Enqueue<T>(Func<Task<T>> callback, CancellationToken cancellationToken = default);
    public Task WhenAll();
}
public readonly struct ID : ICoreId<ID>, IEquatable<ID>
{
    public static ID Empty { get; } public Guid Value { get; }
    public ID(Guid id); public ID();
    public static ID Create(); public static ID CreateFromGuid(Guid? id);
    public static bool TryParse(string? value, [NotNullWhen(true)] out ID? id);
    public Guid AsGuid();
    public static implicit operator ID(Guid key);
}
public static class Pool { public static PooledBuffer<T> Rent<T>(int size); }
public sealed class PooledBuffer<T> : IDisposable { public T[] Array { get; } public int Size { get; } public T this[int index] { get; set; } public void Dispose(); public static implicit operator T[](PooledBuffer<T> pool); }

public static partial class Utility
{
    public static IResult Try(Action? callback);
    public static Task<IResult> Try(Func<Task>? callback);
    public static IResult<T> Try<T>(Func<T?>? callback);
    public static Task<IResult<T>> Try<T>(Func<Task<T?>>? callback);
    public static IResult<T> TryResult<T>(Func<IResult<T>> callback);
    public static Task<IResult<T>> TryResult<T>(Func<Task<IResult<T>>> callback);
    public static bool TryChangeType<TValue>(object? value, [NotNullWhen(true)] out TValue? result, CultureInfo? cultureInfo = null);
    public static Task SafeAwait(Task? callback);
    public static ValueTask SafeAwait(ValueTask? callback);
    public static Task<T?> SafeAwait<T>(Task<T>? callback);
    public static string Serialize<T>(T? entity, bool prettyPrint = false);
    public static T? DeserializeObject<T>(string? jsonContent);
    public static IResult<T> DeserializeObject<T>(Stream stream);
    public static IResult SerializeToStream<T>(T entity, Stream stream, Encoding? encoding = null, bool prettyPrint = false);
    public static IResult<string> HashSHA256(string? data, byte[]? key = null);
    public static IResult<string> Crc32(string? data);
    public static int Crc32AsInt(string? data);
    public static IResult<string> ToBase64(byte[]? data);
    public static IResult<byte[]> FromBase64(string? content);
    public static IResult<string> CheckDirectory(string path);
    public static Result FCopy(string source, string destination);
    public static string RemoveInvalid(string path);
}
```

```csharp
namespace CoreOne.Services;

public class BaseService : IDisposable, IAsyncDisposable
{
    [OInject(Optional = true)] protected ILogger<BaseService>? Logger { get; init; }
    protected IServiceProvider? ServiceProvider { get; init; }
    protected AToken Token { get; init; }
    public BaseService(IServiceProvider? services);
    public ValueTask DisposeAsync();
    protected virtual void Dispose(bool disposing);
    protected virtual ValueTask DisposeAsync(bool disposing);
    protected TService Get<TService>() where TService : notnull;
    protected void LogResult<TResult>(TResult? result, string msg, [CallerMemberName] string? name = null) where TResult : IResult;
}

public class Debounce(Action callback, TimeSpan delay) : Debounce<object?>, IDebounce
{
    public Debounce(Action callback, int delayMS);
    public void Invoke(); public void Invoke(bool skipDelay);
}
public class Debounce<TModel>(Action<TModel> callback, TimeSpan delay) : IDebounce<TModel>, IDisposable
{
    public Debounce(Action<TModel> callback, int delayMS);
    public void Invoke(TModel model); public void Invoke(TModel model, bool skipDelay);
    public void Dispose();
}
public class DebounceTrigger(Func<CancellationToken, Task> callback, TimeSpan delay) : DebounceTrigger<object?>, IDebounce
{
    public DebounceTrigger(Func<CancellationToken, Task> callback, int delayMS);
    public void Invoke();
}
public class DebounceTrigger<TModel>(Func<TModel, CancellationToken, Task> callback, TimeSpan delay) : IDebounce<TModel>, IDisposable
{
    public DebounceTrigger(Func<TModel, CancellationToken, Task> callback, int delayMS);
    public DebounceTrigger(Func<TModel, Task> callback, int delayMS);
    public DebounceTrigger(Func<TModel, Task> callback, TimeSpan delay);
    public DebounceTrigger(Action<TModel> callback, TimeSpan delay);
    public void Invoke(TModel model);
    public void Dispose();
}
```

```csharp
namespace CoreOne.Reflection;

public readonly struct Metadata : IEquatable<Metadata>
{
    public static readonly Metadata Empty;
    public bool CanRead { get; } public bool CanWrite { get; } public bool IsStatic { get; }
    public Type FPType { get; } public string Name { get; }
    public InvokeCallback Getter { get; } public Set? Setter { get; }
    public object? GetValue(object? instance);
    public bool SetValue(object? instance, object? value);
    public T? GetCustomAttribute<T>(bool inherits = false) where T : Attribute;
    public IEnumerable<T> GetCustomAttributes<T>(bool inherits = false) where T : Attribute;
    public bool TryGetAttribute<T>([NotNullWhen(true)] out T? attribute) where T : Attribute;
    public FieldInfo? AsFieldInfo(); public PropertyInfo? AsPropertyInfo();
}

public static class MetaType
{
    public const BindingFlags FLAGS; public const BindingFlags INSTANCE_FLAGS;
    public static Metadata GetMetadata(Type? type, string? propertyName, BindingFlags flags = FLAGS);
    public static IReadOnlyList<Metadata> GetMetadatas(Type type, BindingFlags flags = INSTANCE_FLAGS);
    public static InvokeCallback GetHandler(Type type, string? propertyName, BindingFlags flags = FLAGS);
    public static Set? SetHandler(Type type, string propertyName, BindingFlags flags = FLAGS);
    public static InvokeCallback GetInvokeMethod(MethodInfo? method);
    public static InvokeCallback GetInvokeMethod(Type? type, string name, params Type[] args);
    public static InvokeCallback GetInvokestaticMethod(Type type, string name, params Type[] args);
    public static string GetName<TProperty>(this Expression<Func<TProperty>>? expression);
    public static string? GetName<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda);
    public static PropertyInfo? GetPropertyInfo<TProperty>(Expression<Func<TProperty>> expression);
    public static Type? GetUnderlyingType(Type? type);
}

public static class TypeUtility
{
    public static IResult<T> BasicMap<T>(object? instance);
    public static IResult<object> BasicMap(object? instance, Type toType);
    public static object GetInstance(this Type type);
    public static object GetInstance<T1>(this Type type, T1 t1);
    public static object GetInstance<T1, T2>(this Type type, T1 t1, T2 t2);
    public static object GetInstance<T1, T2, T3>(this Type type, T1 t1, T2 t2, T3 t3);
    public static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck);
    public static bool IsSystemType(Type type);
}

public static class ServiceInitializer { public static void Initialize(object instance, IServiceProvider? services); }
```

---

## `CoreOne.Http`, `CoreOne.Generators`, & `CoreOne.Identity`

```csharp
namespace CoreOne.Http.Attributes;

[AttributeUsage(AttributeTargets.Interface)]
public class RestClientAttribute(string? basePath = null, string? clientName = null, string? serializerKey = null) : Attribute;
public class GetAttribute(string? path = null) : HttpMethodAttribute("GET", path);
public class PostAttribute(string? path = null) : HttpMethodAttribute("POST", path);
public class PutAttribute(string? path = null) : HttpMethodAttribute("PUT", path);
public class PatchAttribute(string? path = null) : HttpMethodAttribute("PATCH", path);
public class DeleteAttribute(string? path = null) : HttpMethodAttribute("DELETE", path);
public class HeadAttribute(string? path = null) : HttpMethodAttribute("HEAD", path);
public class OptionsAttribute(string? path = null) : HttpMethodAttribute("OPTIONS", path);
public class QueryAttribute(string? path = null) : HttpMethodAttribute("QUERY", path);

public class BodyAttribute(BodySerializationMethod serializationMethod = BodySerializationMethod.Serialized, string? serializerKey = null) : Attribute;
public class QueryParamAttribute(string? alias = null, string? format = null) : Attribute;
public class HeaderAttribute(string name) : Attribute;
public class HeadersAttribute(params string[] headers) : Attribute;
public class AuthorizeAttribute(string scheme = "Bearer") : Attribute;
public class MultipartAttribute(string boundaryText = "----CoreOneMultipartBoundary") : Attribute;
public class AliasAsAttribute(string name) : Attribute;
public class SerializerAttribute(string key) : Attribute { public SerializerAttribute(Type serializerType); }
public class SerializerAttribute<TSerializer>() : SerializerAttribute(typeof(TSerializer)) where TSerializer : ISerializer;

namespace CoreOne.Http.Extensions;
public static class HttpServiceCollectionExtensions
{
    public static IHttpClientBuilder AddRestClient<TInterface>(this IServiceCollection services, Action<HttpClient>? configureClient = null, string? serializerKey = null) where TInterface : class;
    public static IHttpClientBuilder AddRestClient<TInterface>(this IServiceCollection services, Action<IServiceProvider, HttpClient>? configureClient, string? serializerKey = null) where TInterface : class;
    public static IHttpClientBuilder AddRestClient<TInterface, TClient>(this IServiceCollection services, Action<IServiceProvider, HttpClient>? configureClient = null, string? serializerKey = null) where TInterface : class where TClient : class, TInterface;
}
```
