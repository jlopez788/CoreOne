namespace OneCore.Services;

public class Debounce(Action callback, TimeSpan delay) : Debounce<object?>(p => callback(), delay)
{
    public Debounce(Action callback, int delayMS) : this(callback, TimeSpan.FromMilliseconds(delayMS)) { }

    public void Invoke() => Invoke(null);
}

public class Debounce<T>(Action<T> callback, TimeSpan delay) : IDisposable
{
    private readonly Action<T> Callback = callback;
    private readonly TimeSpan Delay = delay;
    private CancellationTokenSource? Token = new CancellationTokenSource();

    public Debounce(Action<T> callback, int delayMS) : this(callback, TimeSpan.FromMilliseconds(delayMS)) { }

    public void Dispose()
    {
        Token?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void Invoke(T model)
    {
        if (Delay == TimeSpan.Zero)
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
                    if (t.IsCompletedSuccessfully && !refToken.IsCancellationRequested)
                        Callback.Invoke(model);
                }
                catch { }
            });
    }
}