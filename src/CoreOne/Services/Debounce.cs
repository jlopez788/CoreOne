namespace CoreOne.Services;

public class Debounce(Action callback, TimeSpan delay) : Debounce<object?>(p => callback(), delay)
{
    public Debounce(Action callback, int delayMS) : this(callback, TimeSpan.FromMilliseconds(delayMS)) { }

    public void Invoke() => Invoke(null, false);

    public void Invoke(bool skipDelay) => Invoke(null, skipDelay);
}

public class Debounce<TModel>(Action<TModel> callback, TimeSpan delay) : IDisposable
{
    private readonly Action<TModel> Callback = callback;
    private readonly TimeSpan Delay = delay;
    private CancellationTokenSource? Token = new();

    public Debounce(Action<TModel> callback, int delayMS) : this(callback, TimeSpan.FromMilliseconds(delayMS)) { }

    public void Dispose()
    {
        Token?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Invoke(TModel model) => Invoke(model, false);

    public void Invoke(TModel model, bool skipDelay)
    {
        if (Delay == TimeSpan.Zero || skipDelay)
        {
            Callback.Invoke(model);
            return;
        }

        Token?.Cancel();
        Token = new CancellationTokenSource();

        var refToken = Token.Token;
        Task.Delay(Delay, refToken)
            .ContinueWith(t => {
                try
                {
#if NET9_0_OR_GREATER
                    if (t.IsCompletedSuccessfully && !refToken.IsCancellationRequested)
                        Callback.Invoke(model);
#else
                    if (t.IsCompleted && !refToken.IsCancellationRequested)
                        Callback.Invoke(model);
#endif
                }
                catch { }
            });
    }
}