namespace CoreOne.Reactive;

public sealed class BehaviorSubject<T>(T? value = default) : Subject<T>
{
    private readonly Lock Sync = new();
    private T? CurrentValue = value;

    public T? Value {
        get {
            using (Sync.EnterScope())
            {
                return Exception != null ? throw Exception : CurrentValue;
            }
        }
    }

    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        using (Sync.EnterScope())
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
        using (Sync.EnterScope())
            CurrentValue = default;

        base.OnDispose();
    }

    protected override void OnNextCore(T next)
    {
        using (Sync.EnterScope())
            CurrentValue = next;
        base.OnNextCore(next);
    }

    protected override void OnSubscribe(IObserver<T> observer)
    {
        if (Value is not null)
            observer.OnNext(Value);
    }
}