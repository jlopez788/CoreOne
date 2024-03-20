namespace OneCore.Comparers;

public sealed class ReferenceEqualityComparer : IEqualityComparer<object>, IEqualityComparer
{
    public static readonly ReferenceEqualityComparer Default = new();

    private ReferenceEqualityComparer()
    { }

    public new bool Equals(object? x, object? y)
    {
        var flag = (x is null && y is null) || ReferenceEquals(x, y);
        if (!flag && x is not null && y is not null && x.GetType() == y.GetType())
            flag = x.Equals(y);
        return flag;
    }

    public int GetHashCode(object? obj) => obj?.GetHashCode() ?? 0;
}

public sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
{
    public static readonly ReferenceEqualityComparer<T> Default = new();

    private ReferenceEqualityComparer()
    { }

    public bool Equals(T? x, T? y)
    {
        var flag = (x is null && y is null) || ReferenceEquals(x, y);
        if (!flag && x is not null && y is not null && x.GetType() == y.GetType())
            flag = x.Equals(y);
        return flag;
    }

    public int GetHashCode(T? obj) => obj?.GetHashCode() ?? 0;
}