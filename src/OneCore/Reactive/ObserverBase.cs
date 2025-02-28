namespace OneCore.Reactive;

public abstract class ObserverBase<T> : Disposable, IObserver<T>, IDisposable
{
    private readonly Lock Sync = new();
    protected Exception? Exception { get; private set; }
    private bool IsFinalized;

    protected ObserverBase()
    { }

    /// <summary>
    ///  Notifies the observer that the provider has finished sending push-based notifications.
    /// </summary>
    public void OnCompleted()
    {
        using (Sync.EnterScope())
        {
            Exception = null;
            IsFinalized = true;
        }

        OnCompletedCore();
    }

    /// <summary>
    /// Notifies the observer that the provider has experienced an error condition.
    /// </summary>
    /// <param name="exception"></param>
    public void OnError(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        using (Sync.EnterScope())
        {
            Exception = exception;
            IsFinalized = true;
        }

        OnErrorCore(exception);
    }

    /// <summary>
    /// Provides the observer with new data.
    /// </summary>
    /// <param name="value"></param>
    public void OnNext(T value)
    {
        if (!IsFinalized)
        {
            using (Sync.EnterScope())
                OnNextCore(value);
        }
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