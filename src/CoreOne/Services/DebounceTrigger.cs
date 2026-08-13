namespace CoreOne.Services;

public class DebounceTrigger(Func<CancellationToken, Task> callback, TimeSpan delay) : DebounceTrigger<object?>((_, ct) => callback(ct), delay)
{
    public DebounceTrigger(Func<CancellationToken, Task> callback, int delayMS) : this(callback, TimeSpan.FromMilliseconds(delayMS)) { }
}

/// <summary>Fires immediately on first trigger, then debounces subsequent triggers by <paramref name="delay"/>.</summary>
public class DebounceTrigger<TModel>(Func<TModel, CancellationToken, Task> callback, TimeSpan delay) : IDebounce<TModel>, IDisposable
{
    private readonly SafeLock Lock = new();
    private bool HasFirstFired;
    private SToken Token = SToken.Create();

    public DebounceTrigger(Func<TModel, CancellationToken, Task> callback, int delayMS) : this(callback, TimeSpan.FromMilliseconds(delayMS)) { }

    public DebounceTrigger(Func<TModel, Task> callback, int delayMS) : this((p, _) => callback.Invoke(p), TimeSpan.FromMilliseconds(delayMS)) { }

    public DebounceTrigger(Func<TModel, Task> callback, TimeSpan delay)
        : this((p, _) => callback(p), delay) { }

    public DebounceTrigger(Action<TModel> callback, TimeSpan delay)
        : this((p, _) => {
            callback(p);
            return Task.CompletedTask;
        }, delay)
    { }

    public void Dispose()
    {
        using (Lock.EnterScope())
        {
            Token.Cancel();
            Token.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// First call executes immediately. Subsequent calls reset the debounce window,
    /// cancelling any pending or in-progress callback.
    /// </summary>
    public void Invoke(TModel model)
    {
        SToken token;
        bool isFirst;
        using (Lock.EnterScope())
        {
            Token.Cancel();
            Token = SToken.Create();
            token = Token;
            isFirst = !HasFirstFired;
            if (isFirst)
                HasFirstFired = true;
        }

        _ = isFirst
            ? Task.Run(() => callback(model, token))
            : Task.Run(async () => {
                try
                {
                    await Task.Delay(delay, token);
                    if (!token.IsCancellationRequested)
                        await callback(model, token);
                }
                catch (OperationCanceledException) { }
            });
    }
}