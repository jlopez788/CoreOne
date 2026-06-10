namespace CoreOne.Extensions;

public static class ResultExtensions
{
    public static async Task<IResult<T>> ExecuteAsync<T>(this Task<IResult<T>> task, Action<IResult<T>> callback)
    {
        var result = await task;
        var next = Utility.Try(() => callback.Invoke(result));
        return next.Success ? result : next.ToResult<T>();
    }

    public static async Task<IResult<T>> ExecuteAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, Task> callback)
    {
        var result = await task;
        var next = await Utility.Try(() => callback.Invoke(result));
        return next.Success ? result : next.ToResult<T>();
    }

    public static T? Match<T>(this IResult<T> result, Func<T?> onFailure)
    {
        return result.Success && result.Model is not null ? result.Model : onFailure();
    }

    public static TResult? Match<T, TResult>(this IResult<T> result, Func<T, TResult?> onSuccess, Func<TResult?> onFailure)
    {
        return result.Success && result.Model is not null ? onSuccess(result.Model) : onFailure();
    }

    public static T? Match<T>(this IResult<T> result, Func<string?, T?> onFailure)
    {
        return result.Success && result.Model is not null ? result.Model : onFailure(result.Message);
    }

    public static async Task<T> MatchAsync<T>(this Task<IResult<T>> task, Func<T> onNullModel)
    {
        var result = await task;
        return result.Success && result.Model is not null ? result.Model : onNullModel();
    }

    public static async Task<TResult> MatchAsync<T, TResult>(this Task<IResult<T>> task, Func<T, TResult> callback, Func<TResult> onNullModel)
    {
        var result = await task;
        return result.Success && result.Model is not null ? callback(result.Model) : onNullModel();
    }

    public static async Task<T?> MatchAsync<T>(this Task<IResult<T>> task, Func<string?, T?> onFailure)
    {
        var result = await task;
        return result.Success && result.Model is not null ? result.Model : onFailure(result.Message);
    }

    public static async Task<TResult?> MatchAsync<T, TResult>(this Task<IResult<T>> task, Func<T, TResult?> onSuccess, Func<string?, TResult?> onFailure)
    {
        var result = await task;
        return result.Success && result.Model is not null ? onSuccess(result.Model) : onFailure(result.Message);
    }

    public static async Task<IResult<T>> OnFailAsync<T>(this Task<IResult<T>> task, Action<IResult<T>> callback)
    {
        var result = await task;
        if (!result.Success || result.Model is null)
            callback.Invoke(result);
        return result;
    }

    public static async Task<IResult<T>> OnFailSelectAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, T?> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            result :
            Utility.Try(() => callback.Invoke(result));
    }

    public static async Task<IResult<T>> OnFailSelectAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, Task<T?>> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            result :
            await Utility.Try(() => callback.Invoke(result));
    }

    public static async Task<IResult<T>> OnFailSelectResultAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, IResult<T>> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            result :
            Utility.TryResult(() => callback.Invoke(result));
    }

    public static async Task<IResult<T>> OnFailSelectResultAsync<T>(this Task<IResult<T>> task, Func<IResult<T>, Task<IResult<T>>> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            result :
            await Utility.TryResult(() => callback.Invoke(result));
    }

    public static IResult OnSuccess(this IResult result, Action callback) => result.Success ? Utility.Try(callback) : result;

    public static IResult<T> OnSuccess<T>(this IResult<T> result, Action<T> callback)
    {
        if (result.Success && result.Model is not null)
        {
            Utility.Try(() => callback(result.Model));
        }

        return result;
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(this Task<HttpResult<T>> task, Action<T> callback)
    {
        var result = await task;
        if (result.Success && result.Model is not null)
        {
            var next = Utility.Try(() => callback.Invoke(result.Model));
            return next.ToResult(result.Model);
        }
        return result;
    }

    public static async Task<IResult<T>> OnSuccessAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, TResult?> callback)
    {
        var result = await task;
        if (result.Success && result.Model is not null)
        {
            var next = Utility.Try(() => callback.Invoke(result.Model));
            return next.ToResult(result.Model);
        }
        return result;
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(this IResult<T> result, Func<T, Task> callback)
    {
        if (result.Success && result.Model is not null)
        {
            var next = await Utility.Try(() => callback.Invoke(result.Model));
            return next.ToResult(result.Model);
        }

        return result;
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(this Task<IResult<T>> task, Action<T> callback)
    {
        var result = await task;
        if (result.Success && result.Model is not null)
        {
            var next = Utility.Try(() => callback.Invoke(result.Model));
            return next.ToResult(result.Model);
        }

        return result;
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(this Task<IResult<T>> task, Func<T, Task> callback)
    {
        var result = await task;
        if (result.Success && result.Model is not null)
        {
            var next = await Utility.Try(() => callback.Invoke(result.Model));
            return next.ToResult(result.Model);
        }

        return result;
    }

    public static IResult<TResult> Select<TResult>(this IResult result, Func<TResult> callback)
    {
        return result.Success ?
            Utility.Try(() => callback()) :
            result.ToResult<TResult>();
    }

    public static IResult<TResult> Select<T, TResult>(this IResult<T> result, Func<T, TResult> callback)
    {
        return result.Success && result.Model is not null ?
            Utility.Try(() => callback(result.Model)) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectAsync<TResult>(this IResult result, Func<Task<TResult?>> callback)
    {
        return result.Success ?
            await Utility.Try(() => callback.Invoke()) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectAsync<TResult>(this Task<IResult> task, Func<Task<TResult?>> callback)
    {
        var result = await task;
        return result.Success ?
            await Utility.Try(() => callback.Invoke()) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectAsync<T, TResult>(this IResult<T> result, Func<T, Task<TResult?>> callback)
    {
        return result.Success && result.Model is not null ?
            await Utility.Try(() => callback.Invoke(result.Model)) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectAsync<T, TResult>(this Task<IResult<T>> task, Func<T, TResult?> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            Utility.Try(() => callback.Invoke(result.Model)) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectAsync<T, TResult>(this Task<IResult<T>> task, Func<T, Task<TResult?>> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            await Utility.Try(() => callback.Invoke(result.Model)) :
            result.ToResult<TResult>();
    }

    public static IResult<TResult> SelectResult<T, TResult>(this IResult<T> result, Func<T, IResult<TResult>> callback)
    {
        return result.Success && result.Model is not null ?
            Utility.TryResult(() => callback(result.Model)) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectResultAsync<T, TResult>(this IResult<T> result, Func<T, Task<IResult<TResult>>> callback)
    {
        return result.Success && result.Model is not null ?
            await Utility.TryResult(() => callback.Invoke(result.Model)) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<IResult<T>> task, Func<T, IResult<TResult>> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            Utility.TryResult(() => callback.Invoke(result.Model)) :
            result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<IResult<T>> task, Func<T, Task<IResult<TResult>>> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            await Utility.TryResult(() => callback.Invoke(result.Model)) :
            result.ToResult<TResult>();
    }

    public static IResult<T> ToResult<T>(this IResult result, T? model = default) => new Result<T> {
        ResultType = result.ResultType,
        Message = result.Message,
        Model = model ?? default
    };

    public static IResult<T> Where<T>(this IResult<T> result, Func<T, bool> predicate, string? errorMessage = null)
    {
        return result.Success && result.Model is not null ?
            predicate(result.Model) ?
                result :
                new Result<T> {
                    ResultType = ResultType.Fail,
                    Message = errorMessage ?? "Predicate not satisfied.",
                    Model = default
                } :
                new Result<T> {
                    ResultType = result.ResultType,
                    Message = result.Message ?? errorMessage ?? "Invalid state",
                    Model = default
                };
    }

    public static async Task<IResult<T>> WhereAsync<T>(this Task<IResult<T>> task, Func<T, bool> predicate, string? errorMessage = null)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            predicate(result.Model) ?
                result :
                new Result<T> {
                    ResultType = ResultType.Fail,
                    Message = errorMessage ?? "Predicate not satisfied.",
                    Model = default
                } :
                new Result<T> {
                    ResultType = result.ResultType,
                    Message = result.Message ?? errorMessage ?? "Invalid state",
                    Model = default
                };
    }

    public static async Task<IResult<T>> WhereAsync<T>(this Task<IResult<T>> task, Func<T, bool> predicate, Func<T?, string?>? getError = null)
    {
        var result = await task;
        return result.Success && result.Model is not null ?
            predicate(result.Model) ?
                result :
                new Result<T> {
                    ResultType = ResultType.Fail,
                    Message = getError?.Invoke(result.Model) ?? "Predicate not satisfied.",
                    Model = default
                } :
                new Result<T> {
                    ResultType = result.ResultType,
                    Message = result.Message ?? getError?.Invoke(result.Model) ?? "Invalid state",
                    Model = default
                };
    }
}