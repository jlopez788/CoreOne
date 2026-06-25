using CoreOne.Operations;

namespace CoreOne.Extensions;

public static class CursorResultExtensions
{
    public static Task<CursorResult<TModel>> OnSuccessAsync<TModel>(this Task<CursorResult<TModel>> task, Action<ICollection<TModel>> callback) => task.OnSuccessAsync(items => {
        callback.Invoke(items);
        return Task.CompletedTask;
    });

    public static async Task<CursorResult<TModel>> OnSuccessAsync<TModel>(this Task<CursorResult<TModel>> task, Func<ICollection<TModel>, Task> callback)
    {
        var result = await task;
        if (result.Success && result.Items is not null)
        {
            await callback.Invoke(result.Items);
        }

        return result;
    }

    public static Task<CursorResult<TModel>> OnSuccessAsync<TModel>(this Task<CursorResult<TModel>> task, Action<ICollection<TModel>, string?> callback) => task.OnSuccessAsync((items, cursor) => {
        callback.Invoke(items, cursor);
        return Task.CompletedTask;
    });

    public static async Task<CursorResult<TModel>> OnSuccessAsync<TModel>(this Task<CursorResult<TModel>> task, Func<ICollection<TModel>, string?, Task> callback)
    {
        var result = await task;
        if (result.Success && result.Items is not null)
        {
            await callback.Invoke(result.Items, result.NextCursor);
        }

        return result;
    }

    public static async Task<CursorResult<TResult>> SelectAsync<TModel, TResult>(this Task<CursorResult<TModel>> task, Func<TModel, TResult> selector)
    {
        var result = await task;
        return result.Success && result.Items is not null ?
            new CursorResult<TResult> {
                Items = result.Items.SelectList(selector),
                NextCursor = result.NextCursor,
                Message = result.Message,
                ResultType = result.ResultType
            } :
            new CursorResult<TResult> {
                Items = null,
                NextCursor = null,
                Message = result.Message,
                ResultType = result.ResultType
            };
    }

    public static async Task<CursorResult<TResult>> SelectCollectionAsync<TModel, TResult>(this Task<CursorResult<TModel>> task, Func<ICollection<TModel>, ICollection<TResult>> callback)
    {
        var result = await task;
        return result.Success && result.Items is not null ?
            new CursorResult<TResult> {
                Items = callback.Invoke(result.Items),
                NextCursor = result.NextCursor,
                Message = result.Message,
                ResultType = result.ResultType
            } :
            new CursorResult<TResult> {
                Items = null,
                NextCursor = null,
                Message = result.Message,
                ResultType = result.ResultType
            };
    }

    public static async Task<CursorResult<TResult>> SelectCollectionAsync<TModel, TResult>(this Task<CursorResult<TModel>> task, Func<ICollection<TModel>, Task<ICollection<TResult>?>> callback)
    {
        var result = await task;
        if (result.Success && result.Items is not null)
        {
            var next = await Utility.Try(() => callback.Invoke(result.Items));
            return new CursorResult<TResult>(next.Model, result.NextCursor) {
                ResultType = next.ResultType,
                Message = next.Message
            };
        }

        return new CursorResult<TResult> {
            ResultType = result.ResultType,
            Message = result.Message
        };
    }
}