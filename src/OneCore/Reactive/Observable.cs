namespace OneCore.Reactive;

public static class Observable
{
    private partial class AnonymousObserver<T>(Action<T> onNext, Action? onComplete) : Subject<T>
    {
        private readonly Action<T> Callback = onNext;
        private readonly Action? Completed = onComplete;

        public override string ToString() => $"{nameof(AnonymousObserver<T>)}:: ({Observers.Count})";

        protected override void OnCompletedCore() => Completed?.Invoke();

        protected override void OnNextCore(T value) => Callback.Invoke(value);
    }

    private class DistinctObserver<T, TKey> : Subject<T>
    {
        private readonly BackingField<TKey> BakField;

        public DistinctObserver(IObservable<T> observer, Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        {
            var token = SToken.Create();
            token.Register(this);
            comparer ??= ReferenceEqualityComparer<TKey>.Default;
            BakField = new BackingField<TKey>((x, y) => comparer.Equals(x, y) ? 0 : -1);

            observer.Subscribe(p => {
                var key = keySelector.Invoke(p);
                if (BakField.UpdateValue(key))
                    OnNext(p);
            }, token.Dispose, token);
        }

        public override string ToString() => $"{nameof(DistinctObserver<T, TKey>)}:: ({Observers.Count})";
    }

    private class FilterObserver<T> : Subject<T>
    {
        public FilterObserver(IObservable<T> source, Func<T, bool> predicate)
        {
            var token = SToken.Create();
            token.Register(this);
            source.Subscribe(p => {
                if (predicate(p))
                    OnNext(p);
            }, token.Dispose, token);
        }

        public override string ToString() => $"{nameof(FilterObserver<T>)}:: ({Observers.Count})";
    }

    private class SelectObserver<TSource, TResult> : Subject<TResult>
    {
        public SelectObserver(IObservable<TSource> source, Func<TSource, TResult> selector)
        {
            var token = SToken.Create();
            token.Register(this);
            source.Subscribe(next => OnNext(selector(next)), token.Dispose, token);
        }

        public override string ToString() => $"{nameof(SelectObserver<TSource, TResult>)}:: ({Observers.Count})";
    }

    private class ThrottleObserver<T> : Subject<T>
    {
        protected Debounce<T> Debounce { get; }

        public ThrottleObserver(IObservable<T> observer, TimeSpan duration)
        {
            var token = SToken.Create();
            token.Register(this);
            Debounce = new(OnNext, duration);
            observer.Subscribe(next => Debounce.Invoke(next), token.Dispose, token);
        }

        public override string ToString() => $"{nameof(ThrottleObserver<T>)}:: ({Observers.Count})";
    }

    public static IObservable<TSource> Distinct<TSource>(this IObservable<TSource> source, IEqualityComparer<TSource>? comparer = null) => Distinct(source, p => p, comparer);

    public static IObservable<TSource> Distinct<TSource, TKey>(this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer = null) => new DistinctObserver<TSource, TKey>(source, keySelector, comparer);

    public static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector) => new SelectObserver<TSource, TResult>(source, selector);

    public static void Subscribe<T>(this IObservable<T> source, Action<T> onNext, CancellationToken token) => Subscribe(source, onNext, null, token);

    public static void Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action? oncomplete, CancellationToken token)
    {
        var sub = source.Subscribe(new AnonymousObserver<T>(onNext, oncomplete));
        if (token != SToken.Empty)
            token.Register(sub.Dispose);
    }

    public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, int durationMs) => new ThrottleObserver<TSource>(source, TimeSpan.FromMilliseconds(durationMs));

    public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, TimeSpan duration) => new ThrottleObserver<TSource>(source, duration);

    public static IObservable<T> Where<T>(this IObservable<T> source, Func<T, bool> predicate) => new FilterObserver<T>(source, predicate);
}