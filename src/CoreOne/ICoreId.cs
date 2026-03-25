namespace CoreOne;

public interface ICoreId<T, TKey> : IEquatable<T> where T : ICoreId<T, TKey> where TKey : IComparable<TKey>
{
    TKey Value { get; }

#if NET9_0_OR_GREATER
    abstract static T Empty { get; }

    static abstract bool TryParse(string? value, [NotNullWhen(true)] out T? id);

    static virtual implicit operator bool([NotNullWhen(true)] T? id) => id is not null;

    static abstract implicit operator T(TKey value);

#endif
}

public interface ICoreId<T> : ICoreId<T, Guid> where T : ICoreId<T>
{
#if NET9_0_OR_GREATER

    public static bool Equals(T? left, T? right)
    {
        var empty = Guid.Empty;
        return (left is null && right is null) ||
            ((left is null || left.Value == empty) && (right is null || right.Value == empty)) ||
            (left is not null && right is not null && left.Value == right.Value);
    }

    public static virtual bool operator !=(T? left, T? right) => !Equals(left, right);

    public static virtual bool operator <(T? left, T? right) => Equals(left, right) || (left && right && left.Value < right.Value);

    public static virtual bool operator <=(T? left, T? right) => Equals(left, right) || (left && right && left.Value <= right.Value);

    public static virtual bool operator ==(T? left, T? right) => Equals(left, right);

    public static virtual bool operator >(T? left, T? right) => Equals(left, right) || (left && right && left.Value > right.Value);

    public static virtual bool operator >=(T? left, T? right) => Equals(left, right) || (left && right && left.Value >= right.Value);

#endif
}