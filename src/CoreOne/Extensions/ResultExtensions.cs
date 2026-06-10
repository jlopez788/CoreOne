namespace CoreOne.Extensions;

public static class ResultExtensions
{
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

    public static IResult<TResult> Select<T, TResult>(this IResult<T> result, Func<T, TResult> callback)
    {
        return result.Success && result.Model is not null ?
            Utility.Try(() => callback(result.Model)) :
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
}