namespace CoreOne.Extensions;

public static class SemaphoneSlimExtensions
{
    public static void WaitNext(this SemaphoreSlim semaphore, Action next, CancellationToken cancellationToken = default) => semaphore.WaitNext(() => {
        next();
        return 1;
    }, cancellationToken);

    public static void WaitNext(this SemaphoreSlim semaphore, Action next, Action final, CancellationToken cancellationToken = default) => semaphore.WaitNext(() => {
        next();
        return 1;
    }, final, cancellationToken);

    public static T WaitNext<T>(this SemaphoreSlim semaphore, Func<T> next, CancellationToken cancellationToken = default) => semaphore.WaitNext(next, null, cancellationToken);

    public static T WaitNext<T>(this SemaphoreSlim semaphore, Func<T> next, Action? final, CancellationToken cancellationToken = default)
    {
        try
        {
            semaphore.Wait(cancellationToken);
            return next();
        }
        finally
        {
            semaphore.Release();
            final?.Invoke();
        }
    }

    public static Task WaitNextAsync(this SemaphoreSlim semaphore, Action next, CancellationToken cancellationToken = default) => semaphore.WaitNextAsync(() => {
        next();
        return Task.FromResult(1);
    }, cancellationToken);

    public static Task WaitNextAsync(this SemaphoreSlim semaphore, Action next, Action final, CancellationToken cancellationToken = default) => semaphore.WaitNextAsync(() => {
        next();
        return Task.FromResult(1);
    }, final, cancellationToken);

    public static Task<T> WaitNextAsync<T>(this SemaphoreSlim semaphore, Func<T> next, CancellationToken cancellationToken = default) => semaphore.WaitNextAsync(() => Task.FromResult(next()), cancellationToken);

    public static Task<T> WaitNextAsync<T>(this SemaphoreSlim semaphore, Func<T> next, Action final, CancellationToken cancellationToken = default) => semaphore.WaitNextAsync(() => Task.FromResult(next()), final, cancellationToken);

    public static Task WaitNextAsync(this SemaphoreSlim semaphore, Func<Task> next, CancellationToken cancellationToken = default) => semaphore.WaitNextAsync(async () => {
        await next();
        return 1;
    }, cancellationToken);

    public static Task WaitNextAsync(this SemaphoreSlim semaphore, Func<Task> next, Action final, CancellationToken cancellationToken = default) => semaphore.WaitNextAsync(async () => {
        await next();
        return 1;
    }, final, cancellationToken);

    public static async Task<T> WaitNextAsync<T>(this SemaphoreSlim semaphore, Func<Task<T>> next, CancellationToken cancellationToken = default)
    {
        T value = default!;
        try
        {
            await semaphore.WaitAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                value = await next();
        }
        finally { semaphore.Release(); }
        return value;
    }

    public static async Task<T> WaitNextAsync<T>(this SemaphoreSlim semaphore, Func<Task<T>> next, Action final, CancellationToken cancellationToken = default)
    {
        T value = default!;
        try
        {
            await semaphore.WaitAsync(cancellationToken);
            if (!cancellationToken.IsCancellationRequested)
                value = await next();
        }
        finally
        {
            semaphore.Release();
            final?.Invoke();
        }
        return value;
    }
}