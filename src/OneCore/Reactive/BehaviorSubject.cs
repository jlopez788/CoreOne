namespace OneCore.Reactive;

public sealed class BehaviorSubject<T>(T? value = default) : Subject<T>
{
    private readonly object Sync = new();
    private T? CurrentValue = value;

    public T? Value {
        get {
            lock (Sync)
            {
                CheckDisposed();

                return Exception != null ? throw Exception : CurrentValue;
            }
        }
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        lock (Sync)
        {
            if (IsDisposed)
            {
                value = default;
                return false;
            }

            if (Exception != null)
            {
                throw Exception;
            }

            value = CurrentValue;
            return value is not null;
        }
    }

    protected override void OnDispose()
    {
        lock (Sync)
            CurrentValue = default;

        base.OnDispose();
    }

    protected override void OnNextCore(T next)
    {
        lock (Sync)
            CurrentValue = next;
        base.OnNextCore(next);
    }

    protected override void OnSubscribe(IObserver<T> observer)
    {
        if (Value is not null)
            observer.OnNext(Value);
    }
}