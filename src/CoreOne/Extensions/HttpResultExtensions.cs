namespace CoreOne.Extensions;

public static class HttpResultExtensions
{
    #region -- Match --

    public static T? Match<T>(this HttpResult<T> result, Func<HttpResult<T>, T?> onFailure)
    {
        return result.Success && result.Model is not null ? result.Model : onFailure(result);
    }

    public static T? Match<T, TError>(this HttpResult<T> result, Func<TError?, T?> onFailure)
    {
        if (result.Success && result.Model is not null)
            return result.Model;

        var error = result.GetContent<TError>();
        return onFailure(error.Model);
    }

    public static T? Match<T, TError>(this HttpResult<T> result, Func<TError?, int, T?> onFailure)
    {
        if (result.Success && result.Model is not null)
            return result.Model;

        var error = result.GetContent<TError>();
        return onFailure(error.Model, result.StatusCode);
    }

    public static TResult? Match<T, TResult>(this HttpResult<T> result, Func<T, TResult?> onSuccess, Func<HttpResult<T>, TResult?> onFailure)
    {
        return result.Success && result.Model is not null ? onSuccess(result.Model) : onFailure(result);
    }

    public static TResult? Match<T, TError, TResult>(this HttpResult<T> result, Func<T, TResult?> onSuccess, Func<TError?, TResult?> onFailure)
    {
        if (result.Success && result.Model is not null)
            return onSuccess(result.Model);

        var error = result.GetContent<TError>();
        return onFailure(error.Model);
    }

    public static TResult? Match<T, TError, TResult>(this HttpResult<T> result, Func<T, TResult?> onSuccess, Func<TError?, int, TResult?> onFailure)
    {
        if (result.Success && result.Model is not null)
            return onSuccess(result.Model);

        var error = result.GetContent<TError>();
        return onFailure(error.Model, result.StatusCode);
    }

    public static async Task<T> MatchAsync<T>(this Task<HttpResult<T>> task, Func<T> onNullModel)
    {
        var result = await task.ConfigureAwait(false);
        return result.Success && result.Model is not null ? result.Model : onNullModel();
    }

    public static async Task<T?> MatchAsync<T>(this Task<HttpResult<T>> task, Func<HttpResult<T>, T?> onFailure)
    {
        var result = await task.ConfigureAwait(false);
        return result.Match(onFailure);
    }

    public static async Task<T?> MatchAsync<T, TError>(this Task<HttpResult<T>> task, Func<TError?, T?> onFailure)
    {
        var result = await task.ConfigureAwait(false);
        return result.Match(onFailure);
    }

    public static async Task<T?> MatchAsync<T, TError>(this Task<HttpResult<T>> task, Func<TError?, int, T?> onFailure)
    {
        var result = await task.ConfigureAwait(false);
        return result.Match(onFailure);
    }

    public static async Task<TResult> MatchAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, TResult> onSuccess, Func<TResult> onNullModel)
    {
        var result = await task.ConfigureAwait(false);
        return result.Success && result.Model is not null ? onSuccess(result.Model) : onNullModel();
    }

    public static async Task<TResult?> MatchAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, TResult?> onSuccess, Func<HttpResult<T>, TResult?> onFailure)
    {
        var result = await task.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    public static async Task<TResult?> MatchAsync<T, TError, TResult>(this Task<HttpResult<T>> task, Func<T, TResult?> onSuccess, Func<TError?, TResult?> onFailure)
    {
        var result = await task.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    public static async Task<TResult?> MatchAsync<T, TError, TResult>(this Task<HttpResult<T>> task, Func<T, TResult?> onSuccess, Func<TError?, int, TResult?> onFailure)
    {
        var result = await task.ConfigureAwait(false);
        return result.Match(onSuccess, onFailure);
    }

    #endregion -- Match --

    #region -- OnFail (HttpResult) --

    public static HttpResult OnFail(this HttpResult result, Action<HttpResult> callback)
    {
        if (!result.Success)
            Utility.Try(() => callback.Invoke(result));

        return result;
    }

    public static HttpResult OnFail(this HttpResult result, int statusCode, Action<HttpResult> callback)
    {
        if (!result.Success && result.StatusCode == statusCode)
            Utility.Try(() => callback.Invoke(result));

        return result;
    }

    public static HttpResult OnFail(this HttpResult result, Func<int, bool> statusPredicate, Action<HttpResult> callback)
    {
        if (!result.Success && statusPredicate(result.StatusCode))
            Utility.Try(() => callback.Invoke(result));

        return result;
    }

    public static HttpResult OnFail<TError>(this HttpResult result, Action<TError?> callback)
    {
        if (!result.Success)
        {
            var error = result.GetContent<TError>();
            Utility.Try(() => callback.Invoke(error.Model));
        }

        return result;
    }

    public static HttpResult OnFail<TError>(this HttpResult result, int statusCode, Action<TError?> callback)
    {
        if (!result.Success && result.StatusCode == statusCode)
        {
            var error = result.GetContent<TError>();
            Utility.Try(() => callback.Invoke(error.Model));
        }

        return result;
    }

    public static HttpResult OnFail<TError>(this HttpResult result, Func<int, bool> statusPredicate, Action<TError?> callback)
    {
        if (!result.Success && statusPredicate(result.StatusCode))
        {
            var error = result.GetContent<TError>();
            Utility.Try(() => callback.Invoke(error.Model));
        }

        return result;
    }

    public static async Task<HttpResult> OnFailAsync(this HttpResult result, Func<HttpResult, Task> callback)
    {
        if (!result.Success)
            await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);

        return result;
    }

    public static async Task<HttpResult> OnFailAsync(this HttpResult result, int statusCode, Func<HttpResult, Task> callback)
    {
        if (!result.Success && result.StatusCode == statusCode)
            await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);

        return result;
    }

    public static async Task<HttpResult> OnFailAsync(this HttpResult result, Func<int, bool> statusPredicate, Func<HttpResult, Task> callback)
    {
        if (!result.Success && statusPredicate(result.StatusCode))
            await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);

        return result;
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this HttpResult result, Func<TError?, Task> callback)
    {
        if (!result.Success)
        {
            var error = result.GetContent<TError>();
            await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this HttpResult result, int statusCode, Func<TError?, Task> callback)
    {
        if (!result.Success && result.StatusCode == statusCode)
        {
            var error = result.GetContent<TError>();
            await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this HttpResult result, Func<int, bool> statusPredicate, Func<TError?, Task> callback)
    {
        if (!result.Success && statusPredicate(result.StatusCode))
        {
            var error = result.GetContent<TError>();
            await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<HttpResult> OnFailAsync(this Task<HttpResult> task, Action<HttpResult> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(callback);
    }

    public static async Task<HttpResult> OnFailAsync(this Task<HttpResult> task, int statusCode, Action<HttpResult> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusCode, callback);
    }

    public static async Task<HttpResult> OnFailAsync(this Task<HttpResult> task, Func<int, bool> statusPredicate, Action<HttpResult> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusPredicate, callback);
    }

    public static async Task<HttpResult> OnFailAsync(this Task<HttpResult> task, Func<HttpResult, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult> OnFailAsync(this Task<HttpResult> task, int statusCode, Func<HttpResult, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusCode, callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult> OnFailAsync(this Task<HttpResult> task, Func<int, bool> statusPredicate, Func<HttpResult, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusPredicate, callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this Task<HttpResult> task, Action<TError?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(callback);
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this Task<HttpResult> task, int statusCode, Action<TError?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusCode, callback);
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this Task<HttpResult> task, Func<int, bool> statusPredicate, Action<TError?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusPredicate, callback);
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this Task<HttpResult> task, Func<TError?, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this Task<HttpResult> task, int statusCode, Func<TError?, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusCode, callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult> OnFailAsync<TError>(this Task<HttpResult> task, Func<int, bool> statusPredicate, Func<TError?, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusPredicate, callback).ConfigureAwait(false);
    }

    #endregion -- OnFail (HttpResult) --

    #region -- OnFail (HttpResult<T>) --

    public static HttpResult<T> OnFail<T>(this HttpResult<T> result, Action<HttpResult<T>> callback)
    {
        if (!result.Success || result.Model is null)
            Utility.Try(() => callback.Invoke(result));

        return result;
    }

    public static HttpResult<T> OnFail<T>(this HttpResult<T> result, int statusCode, Action<HttpResult<T>> callback)
    {
        if ((!result.Success || result.Model is null) && result.StatusCode == statusCode)
            Utility.Try(() => callback.Invoke(result));

        return result;
    }

    public static HttpResult<T> OnFail<T>(this HttpResult<T> result, Func<int, bool> statusPredicate, Action<HttpResult<T>> callback)
    {
        if ((!result.Success || result.Model is null) && statusPredicate(result.StatusCode))
            Utility.Try(() => callback.Invoke(result));

        return result;
    }

    public static HttpResult<T> OnFail<T, TError>(this HttpResult<T> result, Action<TError?> callback)
    {
        if (!result.Success || result.Model is null)
        {
            var error = result.GetContent<TError>();
            Utility.Try(() => callback.Invoke(error.Model));
        }

        return result;
    }

    public static HttpResult<T> OnFail<T, TError>(this HttpResult<T> result, int statusCode, Action<TError?> callback)
    {
        if ((!result.Success || result.Model is null) && result.StatusCode == statusCode)
        {
            var error = result.GetContent<TError>();
            Utility.Try(() => callback.Invoke(error.Model));
        }

        return result;
    }

    public static HttpResult<T> OnFail<T, TError>(this HttpResult<T> result, Func<int, bool> statusPredicate, Action<TError?> callback)
    {
        if ((!result.Success || result.Model is null) && statusPredicate(result.StatusCode))
        {
            var error = result.GetContent<TError>();
            Utility.Try(() => callback.Invoke(error.Model));
        }

        return result;
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this HttpResult<T> result, Func<HttpResult<T>, Task> callback)
    {
        if (!result.Success || result.Model is null)
            await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);

        return result;
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this HttpResult<T> result, int statusCode, Func<HttpResult<T>, Task> callback)
    {
        if ((!result.Success || result.Model is null) && result.StatusCode == statusCode)
            await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);

        return result;
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this HttpResult<T> result, Func<int, bool> statusPredicate, Func<HttpResult<T>, Task> callback)
    {
        if ((!result.Success || result.Model is null) && statusPredicate(result.StatusCode))
            await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);

        return result;
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this HttpResult<T> result, Func<TError?, Task> callback)
    {
        if (!result.Success || result.Model is null)
        {
            var error = result.GetContent<TError>();
            await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this HttpResult<T> result, int statusCode, Func<TError?, Task> callback)
    {
        if ((!result.Success || result.Model is null) && result.StatusCode == statusCode)
        {
            var error = result.GetContent<TError>();
            await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this HttpResult<T> result, Func<int, bool> statusPredicate, Func<TError?, Task> callback)
    {
        if ((!result.Success || result.Model is null) && statusPredicate(result.StatusCode))
        {
            var error = result.GetContent<TError>();
            await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this Task<HttpResult<T>> task, Action<HttpResult<T>> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(callback);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this Task<HttpResult<T>> task, int statusCode, Action<HttpResult<T>> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusCode, callback);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Action<HttpResult<T>> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusPredicate, callback);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this Task<HttpResult<T>> task, Func<HttpResult<T>, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this Task<HttpResult<T>> task, int statusCode, Func<HttpResult<T>, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusCode, callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Func<HttpResult<T>, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusPredicate, callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this Task<HttpResult<T>> task, Action<TError?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(callback);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this Task<HttpResult<T>> task, int statusCode, Action<TError?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusCode, callback);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Action<TError?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFail(statusPredicate, callback);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this Task<HttpResult<T>> task, Func<TError?, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this Task<HttpResult<T>> task, int statusCode, Func<TError?, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusCode, callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult<T>> OnFailAsync<T, TError>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Func<TError?, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnFailAsync(statusPredicate, callback).ConfigureAwait(false);
    }

    #endregion -- OnFail (HttpResult<T>) --

    #region -- OnFailSelect --

    public static HttpResult<T> OnFailSelect<T>(this HttpResult<T> result, Func<HttpResult<T>, T?> callback)
    {
        if (result.Success && result.Model is not null)
            return result;

        var next = Utility.Try(() => callback.Invoke(result));
        return ToRecoveredHttpResult(result, next);
    }

    public static HttpResult<T> OnFailSelect<T>(this HttpResult<T> result, int statusCode, Func<HttpResult<T>, T?> callback)
    {
        if ((result.Success && result.Model is not null) || result.StatusCode != statusCode)
            return result;

        var next = Utility.Try(() => callback.Invoke(result));
        return ToRecoveredHttpResult(result, next);
    }

    public static HttpResult<T> OnFailSelect<T>(this HttpResult<T> result, Func<int, bool> statusPredicate, Func<HttpResult<T>, T?> callback)
    {
        if ((result.Success && result.Model is not null) || !statusPredicate(result.StatusCode))
            return result;

        var next = Utility.Try(() => callback.Invoke(result));
        return ToRecoveredHttpResult(result, next);
    }

    public static HttpResult<T> OnFailSelect<T, TError>(this HttpResult<T> result, Func<TError?, T?> callback)
    {
        if (result.Success && result.Model is not null)
            return result;

        var error = result.GetContent<TError>();
        var next = Utility.Try(() => callback.Invoke(error.Model));
        return ToRecoveredHttpResult(result, next);
    }

    public static HttpResult<T> OnFailSelect<T, TError>(this HttpResult<T> result, int statusCode, Func<TError?, T?> callback)
    {
        if ((result.Success && result.Model is not null) || result.StatusCode != statusCode)
            return result;

        var error = result.GetContent<TError>();
        var next = Utility.Try(() => callback.Invoke(error.Model));
        return ToRecoveredHttpResult(result, next);
    }

    public static HttpResult<T> OnFailSelect<T, TError>(this HttpResult<T> result, Func<int, bool> statusPredicate, Func<TError?, T?> callback)
    {
        if ((result.Success && result.Model is not null) || !statusPredicate(result.StatusCode))
            return result;

        var error = result.GetContent<TError>();
        var next = Utility.Try(() => callback.Invoke(error.Model));
        return ToRecoveredHttpResult(result, next);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T>(this Task<HttpResult<T>> task, Func<HttpResult<T>, T?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFailSelect(callback);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T>(this Task<HttpResult<T>> task, int statusCode, Func<HttpResult<T>, T?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFailSelect(statusCode, callback);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Func<HttpResult<T>, T?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFailSelect(statusPredicate, callback);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T>(this Task<HttpResult<T>> task, Func<HttpResult<T>, Task<T?>> callback)
    {
        var result = await task.ConfigureAwait(false);
        if (result.Success && result.Model is not null)
            return result;

        var next = await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);
        return ToRecoveredHttpResult(result, next);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T>(this Task<HttpResult<T>> task, int statusCode, Func<HttpResult<T>, Task<T?>> callback)
    {
        var result = await task.ConfigureAwait(false);
        if ((result.Success && result.Model is not null) || result.StatusCode != statusCode)
            return result;

        var next = await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);
        return ToRecoveredHttpResult(result, next);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Func<HttpResult<T>, Task<T?>> callback)
    {
        var result = await task.ConfigureAwait(false);
        if ((result.Success && result.Model is not null) || !statusPredicate(result.StatusCode))
            return result;

        var next = await Utility.Try(() => callback.Invoke(result)).ConfigureAwait(false);
        return ToRecoveredHttpResult(result, next);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T, TError>(this Task<HttpResult<T>> task, Func<TError?, T?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFailSelect(callback);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T, TError>(this Task<HttpResult<T>> task, int statusCode, Func<TError?, T?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFailSelect(statusCode, callback);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T, TError>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Func<TError?, T?> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnFailSelect(statusPredicate, callback);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T, TError>(this Task<HttpResult<T>> task, Func<TError?, Task<T?>> callback)
    {
        var result = await task.ConfigureAwait(false);
        if (result.Success && result.Model is not null)
            return result;

        var error = result.GetContent<TError>();
        var next = await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        return ToRecoveredHttpResult(result, next);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T, TError>(this Task<HttpResult<T>> task, int statusCode, Func<TError?, Task<T?>> callback)
    {
        var result = await task.ConfigureAwait(false);
        if ((result.Success && result.Model is not null) || result.StatusCode != statusCode)
            return result;

        var error = result.GetContent<TError>();
        var next = await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        return ToRecoveredHttpResult(result, next);
    }

    public static async Task<HttpResult<T>> OnFailSelectAsync<T, TError>(this Task<HttpResult<T>> task, Func<int, bool> statusPredicate, Func<TError?, Task<T?>> callback)
    {
        var result = await task.ConfigureAwait(false);
        if ((result.Success && result.Model is not null) || !statusPredicate(result.StatusCode))
            return result;

        var error = result.GetContent<TError>();
        var next = await Utility.Try(() => callback.Invoke(error.Model)).ConfigureAwait(false);
        return ToRecoveredHttpResult(result, next);
    }

    #endregion -- OnFailSelect --

    #region -- OnSuccess --

    public static HttpResult OnSuccess(this HttpResult result, Action callback)
    {
        if (result.Success)
        {
            var next = Utility.Try(callback);
            if (!next.Success)
                return new HttpResult(WebCodes.Status500InternalServerError, next.Message) {
                    Uri = result.Uri,
                    Serializer = result.Serializer
                };
        }
        return result;
    }

    public static HttpResult<T> OnSuccess<T>(this HttpResult<T> result, Action<T> callback)
    {
        if (result.Success && result.Model is not null)
        {
            var next = Utility.Try(() => callback.Invoke(result.Model));
            if (!next.Success)
                return new HttpResult<T>(WebCodes.Status500InternalServerError, next.Message) {
                    Uri = result.Uri,
                    Serializer = result.Serializer
                };
        }
        return result;
    }

    public static async Task<HttpResult> OnSuccessAsync(this HttpResult result, Func<Task> callback)
    {
        if (result.Success)
        {
            var next = await Utility.Try(callback).ConfigureAwait(false);
            if (!next.Success)
                return new HttpResult(WebCodes.Status500InternalServerError, next.Message) {
                    Uri = result.Uri,
                    Serializer = result.Serializer
                };
        }
        return result;
    }

    public static async Task<HttpResult> OnSuccessAsync(this Task<HttpResult> task, Action callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnSuccess(callback);
    }

    public static async Task<HttpResult> OnSuccessAsync(this Task<HttpResult> task, Func<Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnSuccessAsync(callback).ConfigureAwait(false);
    }

    public static async Task<HttpResult<T>> OnSuccessAsync<T>(this HttpResult<T> result, Func<T, Task> callback)
    {
        if (result.Success && result.Model is not null)
        {
            var next = await Utility.Try(() => callback.Invoke(result.Model)).ConfigureAwait(false);
            if (!next.Success)
                return new HttpResult<T>(WebCodes.Status500InternalServerError, next.Message) {
                    Uri = result.Uri,
                    Serializer = result.Serializer
                };
        }
        return result;
    }

    public static async Task<HttpResult<T>> OnSuccessAsync<T>(this Task<HttpResult<T>> task, Action<T> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.OnSuccess(callback);
    }

    public static async Task<HttpResult<T>> OnSuccessAsync<T>(this Task<HttpResult<T>> task, Func<T, Task> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.OnSuccessAsync(callback).ConfigureAwait(false);
    }

    #endregion -- OnSuccess --

    #region -- Select --

    public static async Task<HttpResult<TResult>> SelectAsync<T, TResult>(this HttpResult<T> result, Func<T, Task<TResult?>> callback)
    {
        if (result.Success && result.Model is not null)
        {
            var next = await Utility.Try(() => callback.Invoke(result.Model)).ConfigureAwait(false);
            return next.Success
                ? new HttpResult<TResult>(next.Model, result.StatusCode, result.Uri) { Serializer = result.Serializer }
                : new HttpResult<TResult>(WebCodes.Status500InternalServerError, next.Message) { Uri = result.Uri, Serializer = result.Serializer };
        }

        return new HttpResult<TResult>(result.StatusCode, result.Message) {
            Uri = result.Uri,
            ErrorContent = result.ErrorContent,
            Serializer = result.Serializer
        };
    }

    public static async Task<HttpResult<TResult>> SelectAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, TResult?> callback)
    {
        var result = await task.ConfigureAwait(false);
        if (result.Success && result.Model is not null)
        {
            var next = Utility.Try(() => callback.Invoke(result.Model));
            return next.Success
                ? new HttpResult<TResult>(next.Model, result.StatusCode, result.Uri) { Serializer = result.Serializer }
                : new HttpResult<TResult>(WebCodes.Status500InternalServerError, next.Message) { Uri = result.Uri, Serializer = result.Serializer };
        }

        return new HttpResult<TResult>(result.StatusCode, result.Message) {
            Uri = result.Uri,
            ErrorContent = result.ErrorContent,
            Serializer = result.Serializer
        };
    }

    public static async Task<HttpResult<TResult>> SelectAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, Task<TResult?>> callback)
    {
        var result = await task.ConfigureAwait(false);
        return await result.SelectAsync(callback).ConfigureAwait(false);
    }

    public static async Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, IResult<TResult>> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.Success && result.Model is not null
            ? Utility.TryResult(() => callback.Invoke(result.Model))
            : result.ToResult<TResult>();
    }

    public static async Task<IResult<TResult>> SelectResultAsync<T, TResult>(this Task<HttpResult<T>> task, Func<T, Task<IResult<TResult>>> callback)
    {
        var result = await task.ConfigureAwait(false);
        return result.Success && result.Model is not null
            ? await Utility.TryResult(() => callback.Invoke(result.Model)).ConfigureAwait(false)
            : result.ToResult<TResult>();
    }

    #endregion -- Select --

    private static HttpResult<T> ToRecoveredHttpResult<T>(HttpResult<T> original, IResult<T> fallback)
    {
        if (fallback.Success && fallback.Model is not null)
        {
            return new HttpResult<T>(fallback.Model, WebCodes.Status200OK, original.Uri) {
                Serializer = original.Serializer
            };
        }

        var statusCode = fallback.ResultType == ResultType.Exception
            ? WebCodes.Status500InternalServerError
            : original.StatusCode;

        return new HttpResult<T>(statusCode, fallback.Message ?? original.Message) {
            Uri = original.Uri,
            ErrorContent = original.ErrorContent,
            Serializer = original.Serializer
        };
    }
}
