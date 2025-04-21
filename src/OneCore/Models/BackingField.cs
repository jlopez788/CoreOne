using CoreOne.Reactive;

namespace CoreOne.Models;


public class BackingField<T> : Disposable
{
    private static readonly Lazy<Comparison<T>> Method = new(InitializeMethod);
    private readonly IComparer<T>? Comparer;
    private readonly Comparison<T>? Comparison;
    private readonly SemaphoreSlim Semaphore = new(1, 1);
    private readonly Type TComparable = typeof(IComparable<T>);
    public readonly Subject<BackingFieldChangedEventArgs<T>> ValueChanged = new();
    public readonly Subject<BackingFieldChangingEventArgs<T>> ValueChanging = new();
    private bool IsValueSet;
    public bool IgnoreNullValues { get; set; }
    public bool IsChanged { get; protected set; }
    public T? PreviousValue { get; protected set; }
    public T? Value { get; protected set; }

    public BackingField()
    {
        var type = typeof(T);
        Comparison = TComparable.IsAssignableFrom(type) ? Method.Value : null;
        Comparison ??= (x, y) => ReferenceEqualityComparer.Default.Equals(x, y) ? 0 : -1;
    }

    public BackingField(IComparer<T> comparer) : this(default, comparer)
    {
        IsValueSet = false;
    }

    public BackingField(T? defaultValue, IComparer<T> comparer)
    {
        IsValueSet = true;
        Value = defaultValue;
        Comparer = comparer ?? Comparer<T>.Default;
    }

    public BackingField(Comparison<T> comparison) : this(default, comparison)
    {
        IsValueSet = false;
    }

    public BackingField(T? defaultValue, Comparison<T>? comparison = null)
    {
        IsValueSet = true;
        Value = defaultValue;
        Comparison = comparison ?? new Comparison<T>((x, y) => ReferenceEqualityComparer.Default.Equals(x, y) ? 0 : -1);
    }

    public static implicit operator T?(BackingField<T> backingField) => backingField is not null ? backingField.Value : default;

    public void MarkResolved() => IsChanged = false;

    public void SetOriginalValue(T? value)
    {
        Value = value;
        IsValueSet = true;
    }

    public void SubscribeOnValueChanged(Action<BackingFieldChangedEventArgs<T>> callback, CancellationToken cancellationToken)
    {
        ValueChanged.Subscribe(callback, cancellationToken);
    }

    public void SubscribeOnValueChanging(Action<BackingFieldChangingEventArgs<T>> callback, CancellationToken cancellationToken)
    {
        ValueChanging.Subscribe(callback, cancellationToken);
    }

    public bool UpdateValue(T? nextValue)
    {
        var isChanged = Compare(nextValue);
        if (isChanged)
        {
            var args = new BackingFieldChangingEventArgs<T>(Value, nextValue);
            IsValueSet = true;
            OnBeforeUpdateCore(args);
            if (args.Cancel)
                return false;

            OnAfterUpdateCore(nextValue);
        }
        return isChanged;
    }

    public async Task<bool> UpdateValueAsync(T? nextValue, Func<BackingFieldChangingEventArgs<T>, Task>? beforeChange = null)
    {
        var isChanged = Compare(nextValue);
        if (isChanged)
        {
            isChanged = await Semaphore.WaitNextAsync(async () => {
                isChanged = Compare(nextValue); // Compare again... Juuuuuust in case it was updated by another thread
                if (isChanged)
                {
                    var args = new BackingFieldChangingEventArgs<T>(Value, nextValue);
                    IsValueSet = true;
                    OnBeforeUpdateCore(args);
                    if (args.Cancel)
                        return false;

                    if (beforeChange is not null)
                    {
                        await beforeChange.Invoke(args);
                        if (args.Cancel)
                            return false;
                    }

                    OnAfterUpdateCore(nextValue);
                }
                return isChanged;
            }, default);
        }
        return isChanged;
    }

    protected bool Compare(T? nextValue)
    {
        var flag = !IsValueSet ||
            (Value is null && nextValue is not null) ||
            (Value is not null && nextValue is null) ||
            (Comparer is not null && Comparer.Compare(Value, nextValue) != 0) ||
            (Comparison is not null && Value is not null && nextValue is not null && Comparison.Invoke(Value, nextValue) != 0);
        return (!IgnoreNullValues || nextValue is not null) && flag;
    }

    protected virtual void OnBeforeUpdate(BackingFieldChangingEventArgs<T> e)
    {
    }

    private static int Compare<TC>(TC x, TC y) where TC : IComparable<TC> => x is not null && y is not null ? x.CompareTo(y) : -1;

    private static Comparison<T> InitializeMethod()
    {
        var type = typeof(T);
        var method = typeof(BackingField<>)
            .MakeGenericType(type)
            ?.GetMethod(nameof(Compare), BindingFlags.Static | BindingFlags.NonPublic)
            ?.MakeGenericMethod(type);
        var x = Expression.Parameter(type, "x");
        var y = Expression.Parameter(type, "y");
        var call = Expression.Call(method!, [x, y]);
        var lambda = Expression.Lambda<Comparison<T>>(call, [x, y]);
        return lambda.Compile();
    }

    private void OnAfterUpdateCore(T? nextValue)
    {
        IsChanged = true;
        PreviousValue = Value;
        Value = nextValue;
        ValueChanged.OnNext(new BackingFieldChangedEventArgs<T>(Value));
    }

    private void OnBeforeUpdateCore(BackingFieldChangingEventArgs<T> args)
    {
        ValueChanging.OnNext(args);
        if (!args.Cancel)
            OnBeforeUpdate(args);
    }
}
