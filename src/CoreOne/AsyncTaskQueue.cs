using System.Collections.Concurrent;

namespace CoreOne;

public class AsyncTaskQueue(int concurrency = 1)
{
    private interface IAsyncWorker
    {
        Task Execute();
    }

    private class AsyncWorker<T>(Func<Task<T>> worker, TaskCompletionSource<T> promise, CancellationToken cancellationToken) : IAsyncWorker
    {
        public async Task Execute()
        {
            try
            {
                var workerTask = worker();
                var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
                var completed = await Task.WhenAny(workerTask, cancellationTask);
                if (completed == cancellationTask)
                {
                    promise.TrySetCanceled(cancellationToken);
                }
                else
                {
                    promise.TrySetResult(await workerTask);
                }
            }
            catch (OperationCanceledException)
            {
                promise.TrySetCanceled(cancellationToken);
            }
            catch (Exception ex)
            {
                promise.TrySetException(ex);
            }
        }
    }

    private readonly SemaphoreSlim Lock = new(concurrency);
    private readonly ConcurrentQueue<IAsyncWorker> Workers = new();
    private volatile bool _isProcessing;
    private int Count = 0;
    private TaskCompletionSource<int>? IdleSignal;

    public Task Enqueue(Action callback, CancellationToken cancellationToken = default) => Enqueue(() => {
        callback?.Invoke();
        return Task.FromResult(true);
    }, cancellationToken);

    public Task Enqueue(Func<Task> callback, CancellationToken cancellationToken = default) => Enqueue(async () => {
        await callback.Invoke();
        return true;
    }, cancellationToken);

    public Task<T> Enqueue<T>(Func<Task<T>> callback, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref Count);

        if (IdleSignal is null || IdleSignal.Task.IsCompleted)
        {
            Interlocked.Exchange(ref IdleSignal, new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously));
        }

        var promise = new TaskCompletionSource<T>();
        Workers.Enqueue(new AsyncWorker<T>(callback, promise, cancellationToken));
        _ = ProcessQueueAsync(); // Fire-and-forget

        return promise.Task;
    }

    public Task WhenAll()
    { // If no work is pending, return immediately
        return IdleSignal is null || Interlocked.CompareExchange(ref Count, 0, 0) == 0 ? Task.CompletedTask : IdleSignal.Task;
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
        }, () => {
            _isProcessing = false;
            if (Interlocked.Decrement(ref Count) == 0)
            {
                IdleSignal?.TrySetResult(0);
            }
        });
    }
}