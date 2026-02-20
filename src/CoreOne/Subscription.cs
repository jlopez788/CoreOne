namespace CoreOne;

public class Subscription(Action callback) : IDisposable
{
    private Action? Callback = callback;

    public void Dispose()
    {
        Interlocked.Exchange(ref Callback, null)?.Invoke();
        GC.SuppressFinalize(this);
    }
}