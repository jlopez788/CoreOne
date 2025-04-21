namespace CoreOne.Extensions;

public static class ResultExtensions
{
    public static IResult OnSuccess(this IResult result, Action? callback) => result.Success ? Utility.Try(callback) : result;

    public static IResult<T> OnSuccess<T>(this IResult<T> result, Action<T?>? callback)
    {
        return result.Success && result.Model is not null ?
            Utility.Try(() => {
                callback?.Invoke(result.Model);
                return result.Model;
            }) : result;
    }

    public static async Task<IResult> OnSuccessAsync(this IResult result, Func<Task>? callback) => result.Success ? await Utility.Try(callback) : result;

    public static Task<IResult<T>> OnSuccessAsync<T>(this IResult<T> result, Func<T?, Task?> callback)
    {
        return result.Success && result.Model is not null ? Utility.Try(async () => {
            await Utility.SafeAwait(callback?.Invoke(result.Model));
            return result.Model;
        }) : TaskFail<T>(result);
    }

    public static Task<IResult<T>> OnSuccessAsync<T>(this Task<IResult<T>> task, Action<T> callback)
    {
        return OnSuccessAsync(task, p => {
            callback.Invoke(p);
            return Task.CompletedTask;
        });
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(this Task<IResult<T>> task, Func<T, Task> callback)
    {
        var result = await task;
        if (result.Success && result.Model is not null)
            callback?.Invoke(result.Model);
        return result;
    }

    public static async Task<IResult<R>> OnSuccessAsync<T, R>(this Task<IResult<T>> task, Func<T, Task<R?>> callback)
    {
        var result = await task;
        return result.Success && result.Model is not null ? await Utility.Try(() => callback.Invoke(result.Model)) : Fail<R>(result);
    }

    public static IResult PipeResult(this IResult result, Func<IResult> calllback)
    {
        try
        { return result.Success ? calllback.Invoke() : result; }
        catch (Exception ex) { return Result.FromException(ex); }
    }

    public static async Task<IResult> PipeResultAsync(this IResult result, Func<Task<IResult>> calllback)
    {
        try
        { return result.Success ? await calllback.Invoke() : result; }
        catch (Exception ex) { return Result.FromException(ex); }
    }

    public static IResult<T> Select<T>(this IResult result, Func<T?> callback) => result.Success ? Utility.Try(callback) : Fail<T>(result);

    public static IResult<R> Select<T, R>(this IResult<T> result, Func<T, R?> callback)
    {
        return result.Success && result.Model is not null ? Utility.Try(() => {
            return callback is not null ? callback.Invoke(result.Model) : default;
        }) : Fail<R>(result);
    }

    public static Task<IResult<T>> SelectAsync<T>(this IResult result, Func<Task<T?>> callback)
    {
        return result.Success ? Utility.Try(callback) : TaskFail<T>(result);
    }

    public static Task<IResult<T>> SelectAsync<T>(this IResult<T> result, Func<T, Task<T?>> callback)
    {
        return result.Success && result.Model is not null ?
            Utility.Try(() => callback.Invoke(result.Model)) : TaskFail<T>(result);
    }

    public static Task<IResult<R>> SelectAsync<T, R>(this IResult<T> result, Func<T, Task<R?>> callback)
    {
        return result.Success && result.Model is not null ?
            Utility.Try(() => callback.Invoke(result.Model)) : TaskFail<R>(result);
    }

    public static async Task<IResult<T>> SelectAsync<T>(this Task<IResult> task, Func<Task<T?>> callback)
    {
        try
        {
            var result = await task;
            return result.Success ? new Result<T>(await callback.Invoke()) : Fail<T>(result);
        }
        catch (Exception ex) { return Result.FromException<T>(ex); }
    }

    public static async Task<IResult<R>> SelectAsync<T, R>(this Task<IResult<T>> task, Func<T, R?> callback)
    {
        try
        {
            var result = await task;
            return result.Success && result.Model is not null ? new Result<R>(callback.Invoke(result.Model)) : Fail<R>(result);
        }
        catch (Exception ex) { return Result.FromException<R>(ex); }
    }

    public static async Task<IResult<R>> SelectAsync<T, R>(this Task<IResult<T>> task, Func<T?, Task<R?>> callback)
    {
        try
        {
            var result = await task;
            return result.Success && result.Model is not null ? new Result<R>(await callback.Invoke(result.Model)) : Fail<R>(result);
        }
        catch (Exception ex) { return Result.FromException<R>(ex); }
    }

    public static IResult<T> SelectResult<T>(this IResult result, Func<IResult<T>> callback)
    {
        return result.Success ? callback.Invoke() : Fail<T>(result);
    }

    public static IResult<R> SelectResult<T, R>(this IResult<T> result, Func<T?, IResult<R>> callback)
    {
        return result.Success ? callback.Invoke(result.Model) : Fail<R>(result);
    }

    public static async Task<IResult<T>> SelectResultAsync<T>(this IResult result, Func<Task<IResult<T>>> callback)
    {
        return result.Success ? await callback.Invoke() : Fail<T>(result);
    }

    public static async Task<IResult<R>> SelectResultAsync<T, R>(this IResult<T> result, Func<T?, Task<IResult<R>>> callback)
    {
        return result.Success ? await callback.Invoke(result.Model) : Fail<R>(result);
    }

    public static async Task<IResult<T>> SelectResultAsync<T>(this Task<IResult> task, Func<Task<IResult<T>>> callback)
    {
        var result = await task;
        return result.Success ? await callback.Invoke() : Fail<T>(result);
    }

    public static async Task<IResult<R>> SelectResultAsync<T, R>(this Task<IResult<T>> task, Func<T?, Task<IResult<R>>> callback)
    {
        var result = await task;
        return result.Success ? await callback.Invoke(result.Model) : Fail<R>(result);
    }

    public static IResult ToResult<T>(this IResult<T> result) => new Result(result.ResultType, result.Message);

    private static IResult<T> Fail<T>(IResult? result = null) => result is null ? Result.Fail<T>("Invalid result") : new Result<T>(result.ResultType, result.Message);

    private static Task<IResult<T>> TaskFail<T>(IResult result) => Task.FromResult(Fail<T>(result));
}