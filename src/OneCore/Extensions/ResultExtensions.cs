namespace OneCore.Extensions;

public static class ResultExtensions
{
    public static IResult OnSuccess(this IResult result, Action? callback)
    {
        if (result.Success)
            callback?.Invoke();
        return result;
    }

    public static IResult<T> OnSuccess<T>(this IResult<T> result, Action<T?>? callback)
    {
        if (result.Success)
            callback?.Invoke(result.Model);
        return result;
    }

    public static async Task<IResult> OnSuccessAsync(this IResult? result, Func<Task?>? callback)
    {
        if (result?.Success == true && callback is not null)
        {
            try
            { await Utility.SafeAwait(callback.Invoke()); }
            catch { }
        }
        return result ?? Result.Fail();
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(this IResult<T> result, Func<T?, Task?> callback)
    {
        IResult<T> next = result;
        try
        {
            if (result is not null)
            {
                next = new Result<T>(result.ResultType, result.Message);
                if (result.Success == true)
                {
                    var task = callback.Invoke(result.Model);
                    if (task is not null)
                        await task;
                }
            }
        }
        catch (Exception ex)
        {
            next = Result.FromException<T>(ex);
        }
        return next;
    }

    public static IResult<T> Select<T>(this IResult result, Func<T?> callback)
    {
        return result.Success ?
            new Result<T>(callback()) :
            new Result<T>(result.ResultType, result.Message);
    }

    public static IResult<R> Select<T, R>(this IResult<T> result, Func<T, R?> callback)
    {
        return result.Success && result.Model is not null ?
            new Result<R>(callback(result.Model)) :
            new Result<R>(result.ResultType, result.Message);
    }

    public static async Task<IResult<T>> SelectAsync<T>(this IResult result, Func<Task<T?>> callback)
    {
        return result.Success ?
            new Result<T>(await callback()) :
            new Result<T>(result.ResultType, result.Message);
    }

    public static async Task<IResult<R>> SelectAsync<T, R>(this IResult<T> result, Func<T, Task<R?>> callback)
    {
        return result.Success && result.Model is not null ?
            new Result<R>(await callback.Invoke(result.Model)) :
            new Result<R>(result.ResultType, result.Message);
    }
}