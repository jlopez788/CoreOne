namespace OneCore.Reactive;

public abstract class ObserverBase<T> : Disposable, IObserver<T>, IDisposable
{
    private readonly object Sync = new();
    protected Exception? Exception { get; private set; }

    protected ObserverBase()
    { }

    /// <summary>
    ///  Notifies the observer that the provider has finished sending push-based notifications.
    /// </summary>
    public void OnCompleted()
    {
        lock (Sync)
            Exception = null;

        OnCompletedCore();
    }

    /// <summary>
    /// Notifies the observer that the provider has experienced an error condition.
    /// </summary>
    /// <param name="exception"></param>
    public void OnError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        lock (Sync)
        {
            CheckDisposed();

            Exception = exception;
        }

        OnErrorCore(exception);
    }

    /// <summary>
    /// Provides the observer with new data.
    /// </summary>
    /// <param name="value"></param>
    public void OnNext(T value)
    {
        lock (Sync)
        {
            CheckDisposed();
            OnNextCore(value);
        }
    }

    protected void CheckDisposed()
    {
        //if (IsDisposed)
        //    throw new ObjectDisposedException(string.Empty);
    }

    protected virtual void OnCompletedCore()
    { }

    protected override void OnDispose()
    {
        OnCompleted();
        base.OnDispose();
    }

    protected virtual void OnErrorCore(Exception exception)
    { }

    protected abstract void OnNextCore(T value);
}