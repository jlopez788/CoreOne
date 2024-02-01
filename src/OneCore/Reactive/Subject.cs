using System.Collections.Immutable;

namespace OneCore.Reactive;

public class Subject<T> : ObserverBase<T>, IObserver<T>, IObservable<T>, IDisposable
{
    private sealed class Subscription(Subject<T> subject, IObserver<T> observer) : IDisposable
    {
        private readonly Subject<T> Subject = subject;
        private IObserver<T>? Observer = observer;

        public void Dispose()
        {
            if (Observer != null)
            {
                lock (Subject.Sync)
                {
                    if (!Subject.IsDisposed && Observer != null)
                    {
                        Subject.Observers = Subject.Observers.Remove(Observer);
                        Observer = null;
                    }
                }
            }
        }
    }

    private readonly object Sync = new();
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
            CheckDisposed();

            Observers = Observers.Add(observer);
            OnSubscribe(observer);
            return new Subscription(this, observer);
        }
    }

    protected override void OnCompletedCore()
    {
        var os = Array.Empty<IObserver<T>>();
        lock (Sync)
        {
            CheckDisposed();

            os = Observers.ToArray();
            Observers = [];
        }
        os.Each(p => p.OnCompleted());
    }

    protected override void OnErrorCore(Exception exception)
    {
        var os = default(IObserver<T>[]);
        lock (Sync)
        {
            CheckDisposed();

            os = Observers.ToArray();
            Observers = [];
        }
        os.Each(p => p.OnError(exception));
    }
    
    protected override void OnNextCore(T value)
    {
        var os = default(IObserver<T>[]);
        lock (Sync)
            os = Observers.ToArray();

        os.Each(p => p.OnNext(value));
    }

    protected virtual void OnSubscribe(IObserver<T> observer)
    { }
}