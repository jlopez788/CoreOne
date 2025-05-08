using System.Collections.Concurrent;

namespace CoreOne;

public class AsyncTaskQueue
{
    private interface IAsyncWorker
    {
        Task Execute();
    }

    private class AsyncWorker<T>(Func<Task<T>> worker, TaskCompletionSource<T> promise) : IAsyncWorker
    {
        public async Task Execute()
        {
            try
            {
                var result = await worker.Invoke();
                promise.SetResult(result);
            }
            catch (Exception ex)
            {
                promise.SetException(ex);
            }
        }
    }

    private readonly SemaphoreSlim Lock = new(1, 1);
    private readonly ConcurrentQueue<IAsyncWorker> Workers = new();
    private volatile bool _isProcessing;

    public Task<T> Enqueue<T>(Func<Task<T>> callback)
    {
        var promise = new TaskCompletionSource<T>();
        Workers.Enqueue(new AsyncWorker<T>(callback, promise));
        _ = ProcessQueueAsync(); // Fire-and-forget

        return promise.Task;
    }

    private async ValueTask ProcessQueueAsync()
    {
        if (_isProcessing)
            return;

        await Lock.WaitNextAsync(async () => {
            _isProcessing = true;

            while (Workers.TryDequeue(out var worker))
                await worker.Execute();
            _isProcessing = false;
        });
    }
}