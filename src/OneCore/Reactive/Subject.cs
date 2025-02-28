using System.Collections.Immutable;

namespace OneCore.Reactive;

public class Subject<T> : ObserverBase<T>, IObserver<T>, IObservable<T>, IDisposable
{
    private readonly Lock Sync = new();
    public bool HasObservers => Observers != null && !Observers.IsEmpty;
    protected ImmutableList<IObserver<T>> Observers { get; set; }

    public Subject() => Observers = [];

    /// <summary>
    /// Notifies the provider that an observer is to receive notifications.
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    public IDisposable Subscribe(IObserver<T> observer)
    {
        if (observer == null)
            return Empty;

        lock (Sync)
        {
            Observers = Observers.Add(observer);
            OnSubscribe(observer);
        }
        return new Subscription(() => {
            if (observer != null)
            {
                using (Sync.EnterScope())
                {
                    if (!IsDisposed && observer != null)
                        Observers = Observers.Remove(observer);
                }
            }
        });
    }

    protected override void OnCompletedCore()
    {
        var os = Array.Empty<IObserver<T>>();
        using (Sync.EnterScope())
        {
            os = [.. Observers];
            Observers = [];
        }
        os.Each(p => p.OnCompleted());
    }

    protected override void OnErrorCore(Exception exception)
    {
        var os = default(IObserver<T>[]);
        using (Sync.EnterScope())
        {
            os = [.. Observers];
            Observers = [];
        }
        os.Each(p => p.OnError(exception));
    }

    protected override void OnNextCore(T value)
    {
        var os = default(IObserver<T>[]);
        using (Sync.EnterScope())
            os = [.. Observers];

        os.Each(p => p.OnNext(value));
    }

    protected virtual void OnSubscribe(IObserver<T> observer)
    { }
}