namespace CoreOne;

public class Disposable : IDisposable
{
    public virtual bool IsDisposed => BakIsDisposed;
    public static readonly IDisposable Empty = new Disposable();
    private volatile bool BakIsDisposed;

    public Disposable() => BakIsDisposed = false;

    ~Disposable()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (!BakIsDisposed)
        {
            BakIsDisposed = true;
            OnDispose();
        }

        GC.SuppressFinalize(this);
    }

    protected virtual void OnDispose()
    {
    }
}