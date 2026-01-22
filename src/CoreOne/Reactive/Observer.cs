namespace CoreOne.Reactive;

public static class Observer
{
    private class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T>? OnNextAction;
        private readonly Action<Exception>? OnErrorAction;
        private readonly Action? OnCompletedAction;

        public AnonymousObserver(Action<T>? onNext, Action<Exception>? onError = null, Action? onCompleted = null)
        {
            OnNextAction = onNext;
            OnErrorAction = onError;
            OnCompletedAction = onCompleted;
        }

        public void OnCompleted() => OnCompletedAction?.Invoke();

        public void OnError(Exception error) => OnErrorAction?.Invoke(error);

        public void OnNext(T value) => OnNextAction?.Invoke(value);
    }

    public static IObserver<T> Create<T>(Action<T> onNext) => new AnonymousObserver<T>(onNext);

    public static IObserver<T> Create<T>(Action<T> onNext, Action<Exception> onError) => new AnonymousObserver<T>(onNext, onError);

    public static IObserver<T> Create<T>(Action<T> onNext, Action<Exception> onError, Action onCompleted) => new AnonymousObserver<T>(onNext, onError, onCompleted);
}
